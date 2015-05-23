using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IDataStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class GroupedTimeEntriesView : ICollectionDataView<object>, IDisposable
    {
        private static readonly string Tag = "GroupedTimeEntriesView";
        private static readonly int ContinueThreshold = 4;

        private readonly List<DateGroup> dateGroups = new List<DateGroup> ();
        private UpdateMode updateMode = UpdateMode.Batch;
        private DateTime startFrom;
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private bool reloadScheduled;
        private bool isLoading;
        private bool hasMore;
        private int lastItemNumber;
        private DateTime lastTimeEntryContinuedTime;

        // for Undo/Restore operations
        private TimeEntryGroup lastRemovedItem;

        public GroupedTimeEntriesView ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
            lastTimeEntryContinuedTime = Time.UtcNow;
            HasMore = true;
            Reload ();
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionDataChange != null) {
                bus.Unsubscribe (subscriptionDataChange);
                subscriptionDataChange = null;
            }
            if (subscriptionSyncFinished != null) {
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        public async void ContinueTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            // Don't continue a new TimeEntry before
            // 4 seconds has passed.
            if (DateTime.UtcNow < lastTimeEntryContinuedTime + TimeSpan.FromSeconds (ContinueThreshold)) {
                return;
            }
            lastTimeEntryContinuedTime = DateTime.UtcNow;

            await entryGroup.Model.ContinueAsync ();

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }

        public async void StopTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            await entryGroup.Model.StopAsync ();

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
        }

        public void RestoreItemFromUndo ()
        {
            if (lastRemovedItem != null) {
                AddTimeEntryGroup (lastRemovedItem);
                lastRemovedItem = null;
            }
        }

        public void RemoveItemWithUndo (TimeEntryGroup data)
        {
            // Remove previous if exists
            RemoveItemPermanently (lastRemovedItem);
            lastRemovedItem = data;
            RemoveTimeEntryGroup (data);
        }

        public void ConfirmItemRemove ()
        {
            RemoveItemPermanently (lastRemovedItem);
        }

        private async void RemoveItemPermanently (TimeEntryGroup itemToRemove)
        {
            if (itemToRemove != null) {
                await itemToRemove.DeleteAsync();
            }
        }

        private async void DeleteTimeGroup (TimeEntryGroup grp)
        {
            await grp.DeleteAsync ();
        }

        private void OnDataChange (DataChangeMessage msg)
        {
            var entry = msg.Data as TimeEntryData;
            if (entry == null) {
                return;
            }

            var isExcluded = entry.DeletedAt != null
                             || msg.Action == DataAction.Delete
                             || entry.State == TimeEntryState.New;

            if (isExcluded) {
                RemoveEntry (entry);
            } else {
                AddOrUpdateEntry (new TimeEntryData (entry));
            }
        }

        private void AddOrUpdateEntry (TimeEntryData entry)
        {
            TimeEntryGroup entryGroup;
            DateGroup dateGroup;
            TimeEntryData existingEntry;
            NotifyCollectionChangedAction entryAction;

            bool isNewEntryGroup;
            bool isNewDateGroup = false;
            int newIndex;
            int groupIndex;
            int oldIndex = -1;

            if (FindExistingEntry (entry, out dateGroup, out entryGroup, out existingEntry)) {
                if (entry.StartTime != existingEntry.StartTime) {
                    var date = entry.StartTime.ToLocalTime ().Date;
                    oldIndex = GetEntryGroupIndex (entryGroup);
                    if (dateGroup.Date != date) {

                        // Remove from containers.
                        entryGroup.Remove (existingEntry);
                        if (entryGroup.Count == 0) {
                            entryGroup.Dispose ();
                            dateGroup.Remove (entryGroup);
                            DispatchCollectionEvent (CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, oldIndex, -1));
                        } else {
                            DispatchCollectionEvent (CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, oldIndex, -1));
                        }

                        // Update old group
                        DispatchCollectionEvent (CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, GetDateGroupIndex (dateGroup), -1));

                        dateGroup = GetDateGroupFor (entry, out isNewDateGroup);
                        entryGroup = GetSuitableEntryGroupFor (dateGroup, entry, out isNewEntryGroup);

                        // In case of new container group, entry is added at creation.
                        if (!isNewEntryGroup) {
                            entryGroup.Add (entry);
                        }

                        Sort ();
                        OnUpdated();

                        entryAction = (isNewEntryGroup) ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
                    } else {
                        entryGroup.Update (entry);
                        dateGroup.Update (entryGroup);
                        Sort ();
                        entryAction = NotifyCollectionChangedAction.Replace;
                    }
                } else {
                    entryGroup.Update (entry);
                    dateGroup.Update (entryGroup);
                    entryAction = NotifyCollectionChangedAction.Replace;
                }
            } else {
                dateGroup = GetDateGroupFor (entry, out isNewDateGroup);
                entryGroup = GetSuitableEntryGroupFor (dateGroup, entry, out isNewEntryGroup);

                // In case of new container group, entry is added at creation.
                if (!isNewEntryGroup) {
                    oldIndex = GetEntryGroupIndex (entryGroup);
                    entryGroup.Add (entry);
                    entryAction = NotifyCollectionChangedAction.Replace;
                } else {
                    entryAction = NotifyCollectionChangedAction.Add;
                }

                Sort ();
            }

            // Update datasource.
            OnUpdated();

            // Update group.
            groupIndex = GetDateGroupIndex (dateGroup);
            var groupAction = isNewDateGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
            DispatchCollectionEvent (CollectionEventBuilder.GetEvent (groupAction, groupIndex, oldIndex));

            // Updated entry.
            newIndex = GetEntryGroupIndex (entryGroup);
            if (entryAction == NotifyCollectionChangedAction.Replace && oldIndex != -1 && oldIndex != newIndex) {
                DispatchCollectionEvent (CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newIndex, oldIndex));
            } else {
                DispatchCollectionEvent (CollectionEventBuilder.GetEvent (entryAction, newIndex, oldIndex));
            }
        }

        private void RemoveEntry (TimeEntryData entry)
        {
            TimeEntryGroup entryGroup;
            DateGroup dateGroup;
            TimeEntryData existingEntry;

            int groupIndex;
            int entryIndex = -1;
            int oldIndex;
            NotifyCollectionChangedAction dateGroupAction = NotifyCollectionChangedAction.Replace;
            NotifyCollectionChangedAction entryGroupAction = NotifyCollectionChangedAction.Replace;

            if (FindExistingEntry (entry, out dateGroup, out entryGroup, out existingEntry)) {
                groupIndex = GetDateGroupIndex (dateGroup);
                oldIndex = GetEntryGroupIndex (entryGroup);

                entryGroup.Remove (existingEntry);
                if (entryGroup.Count == 0) {
                    entryGroup.Dispose ();
                    dateGroup.Remove (entryGroup);
                    entryGroupAction = NotifyCollectionChangedAction.Remove;
                } else {
                    Sort ();
                    entryIndex = GetEntryGroupIndex (entryGroup);
                }

                if (dateGroup.DataObjects.Count == 0) {
                    dateGroups.Remove (dateGroup);
                    dateGroupAction = NotifyCollectionChangedAction.Remove;
                }

                // Update datasource.
                OnUpdated ();

                DispatchCollectionEvent (CollectionEventBuilder.GetEvent (dateGroupAction, groupIndex, -1));
                if (entryIndex != -1 && entryIndex != oldIndex) {
                    DispatchCollectionEvent (CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, entryIndex, oldIndex));
                } else {
                    DispatchCollectionEvent (CollectionEventBuilder.GetEvent (entryGroupAction, oldIndex, -1));
                }
            }
        }

        private void AddTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            const int oldIndex = -1;
            int groupIndex;
            int newIndex;
            bool isNewGroup;
            NotifyCollectionChangedAction entryAction;
            DateGroup grp;

            grp = GetDateGroupFor (entryGroup.Model.Data, out isNewGroup);
            grp.Add (entryGroup);
            Sort ();
            entryAction = NotifyCollectionChangedAction.Add;

            // Update datasource.
            OnUpdated();

            // Update group.
            groupIndex = GetDateGroupIndex (grp);
            var groupAction = isNewGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
            DispatchCollectionEvent (CollectionEventBuilder.GetEvent (groupAction, groupIndex, oldIndex));

            // Updated entry.
            newIndex = GetEntryGroupIndex (entryGroup);
            DispatchCollectionEvent (CollectionEventBuilder.GetEvent (entryAction, newIndex, oldIndex));
        }

        private void RemoveTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            int entryIndex;
            int groupIndex;

            NotifyCollectionChangedAction groupAction = NotifyCollectionChangedAction.Replace;
            DateGroup grp;
            TimeEntryGroup oldEntryGroup;
            TimeEntryData oldEntry;

            if (FindExistingEntry (entryGroup.Model.Data, out grp, out oldEntryGroup, out oldEntry)) {
                entryIndex = GetEntryGroupIndex (oldEntryGroup);
                groupIndex = GetDateGroupIndex (grp);
                grp.Remove (oldEntryGroup);
                if (grp.DataObjects.Count == 0) {
                    dateGroups.Remove (grp);
                    groupAction = NotifyCollectionChangedAction.Remove;
                }

                OnUpdated ();
                DispatchCollectionEvent (CollectionEventBuilder.GetEvent (groupAction, groupIndex, -1));
                DispatchCollectionEvent (CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, entryIndex, -1));
            }
        }

        private bool FindExistingEntry (TimeEntryData dataObject, out DateGroup dateGroup, out TimeEntryGroup existingGroup, out TimeEntryData existingEntry)
        {
            foreach (var grp in dateGroups) {
                foreach (var obj in grp.DataObjects) {
                    TimeEntryData entry;
                    if (obj.Contains (dataObject, out entry)) {
                        dateGroup = grp;
                        existingGroup = obj;
                        existingEntry = entry;
                        return true;
                    }
                }
            }

            existingEntry = null;
            dateGroup = null;
            existingGroup = null;
            return false;
        }

        private DateGroup GetDateGroupFor (TimeEntryData dataObject, out bool isNewDateGroup)
        {
            isNewDateGroup = false;
            var date = dataObject.StartTime.ToLocalTime ().Date;
            var dateGroup = dateGroups.FirstOrDefault (g => g.Date == date);
            if (dateGroup == null) {
                dateGroup = new DateGroup (date);
                dateGroups.Add (dateGroup);
                isNewDateGroup = true;
            }
            return dateGroup;
        }

        private TimeEntryGroup GetExistingEntryGroupFor (DateGroup dateGroup, TimeEntryData dataObject, out bool isNewEntryGroup)
        {
            isNewEntryGroup = false;

            foreach (var grp in dateGroup.DataObjects) {
                TimeEntryData entryData;
                if (grp.Contains (dataObject, out entryData)) {
                    return grp;
                }
            }

            var entryGroup = new TimeEntryGroup (dataObject);
            dateGroup.Add (entryGroup);
            isNewEntryGroup = true;

            return entryGroup;
        }

        private TimeEntryGroup GetSuitableEntryGroupFor (DateGroup dateGroup, TimeEntryData dataObject, out bool isNewEntryGroup)
        {
            isNewEntryGroup = false;

            foreach (var grp in dateGroup.DataObjects) {
                if (grp.CanContain (dataObject)) {
                    return grp;
                }
            }

            var entryGroup = new TimeEntryGroup (dataObject);
            dateGroup.Add (entryGroup);
            isNewEntryGroup = true;

            return entryGroup;
        }

        private int GetEntryGroupIndex (TimeEntryGroup entryGroup)
        {
            int count = 0;
            foreach (var grp in dateGroups) {
                count++;
                // Iterate by entry list.
                foreach (var obj in grp.DataObjects) {
                    if (entryGroup.Model.Data.Matches (obj.Model.Data)) {
                        return count;
                    }
                    count++;
                }
            }
            return -1;
        }

        private int GetDateGroupIndex (DateGroup dateGroup)
        {
            var count = 0;
            foreach (var grp in dateGroups) {
                if (grp.Date == dateGroup.Date) {
                    return count;
                }
                count += grp.DataObjects.Count + 1;
            }
            return -1;
        }

        private void Sort ()
        {
            foreach (var grp in dateGroups) {
                grp.Sort ();
            }

            dateGroups.Sort ((a, b) => b.Date.CompareTo (a.Date));
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            if (reloadScheduled) {
                reloadScheduled = false;
                IsLoading = false;
                Load (true);
            }

            if (subscriptionSyncFinished != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        public event EventHandler Updated;

        private void OnUpdated ()
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }

            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void DispatchCollectionEvent (NotifyCollectionChangedEventArgs args)
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }
            var handler = CollectionChanged;
            if (handler != null) {
                handler (this, args);
            }
        }

        private void BeginUpdate ()
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }
            lastItemNumber = Count;
            updateMode = UpdateMode.Batch;
        }

        private void EndUpdate ()
        {
            updateMode = UpdateMode.Immediate;
            OnUpdated ();
            if (Count > lastItemNumber) {
                DispatchCollectionEvent (CollectionEventBuilder.GetRangeEvent (NotifyCollectionChangedAction.Add, lastItemNumber, Count - lastItemNumber));
            }
        }

        public void Reload ()
        {
            if (IsLoading) {
                return;
            }

            startFrom = Time.UtcNow;
            dateGroups.Clear ();
            HasMore = true;

            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            if (syncManager.IsRunning) {
                // Fake loading until initial sync has finished
                IsLoading = true;
                OnUpdated ();

                reloadScheduled = true;
                if (subscriptionSyncFinished == null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
                }
            } else {
                Load (true);
            }
        }

        public void LoadMore ()
        {
            Load (false);
        }

        private async void Load (bool initialLoad)
        {
            if (IsLoading || !HasMore) {
                return;
            }

            IsLoading = true;
            var client = ServiceContainer.Resolve<ITogglClient> ();
            OnUpdated ();

            try {
                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                var endTime = startFrom;
                var startTime = startFrom = endTime - TimeSpan.FromDays (4);

                bool useLocal = false;

                if (initialLoad) {
                    useLocal = true;
                    startTime = startFrom = endTime - TimeSpan.FromDays (9);
                }

                // Try with latest data from server first:
                if (!useLocal) {
                    const int numDays = 5;
                    try {
                        var minStart = endTime;
                        var jsonEntries = await client.ListTimeEntries (endTime, numDays);

                        BeginUpdate ();
                        var entries = await dataStore.ExecuteInTransactionAsync (ctx =>
                                      jsonEntries.Select (json => json.Import (ctx)).ToList ());

                        // Add entries to list:
                        foreach (var entry in entries) {
                            AddOrUpdateEntry (entry);

                            if (entry.StartTime < minStart) {
                                minStart = entry.StartTime;
                            }
                        }

                        startTime = minStart;
                        HasMore = (endTime.Date - minStart.Date).Days > 0;
                    } catch (Exception exc) {
                        var log = ServiceContainer.Resolve<ILogger> ();
                        if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                            log.Info (Tag, exc, "Failed to fetch time entries {1} days up to {0}", endTime, numDays);
                        } else {
                            log.Warning (Tag, exc, "Failed to fetch time entries {1} days up to {0}", endTime, numDays);
                        }

                        useLocal = true;
                    }
                }

                // Fall back to local data:
                if (useLocal) {
                    var store = ServiceContainer.Resolve<IDataStore> ();
                    var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();

                    var baseQuery = store.Table<TimeEntryData> ()
                                    .OrderBy (r => r.StartTime, false)
                                    .Where (r => r.State != TimeEntryState.New
                                            && r.DeletedAt == null
                                            && r.UserId == userId);
                    var entries = await baseQuery
                                  .QueryAsync (r => r.StartTime <= endTime
                                               && r.StartTime > startTime);

                    BeginUpdate ();
                    foreach (var entry in entries) {
                        AddOrUpdateEntry (entry);
                    }

                    if (!initialLoad) {
                        var count = await baseQuery
                                    .CountAsync (r => r.StartTime <= startTime);
                        HasMore = count > 0;
                    }
                }
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (Tag, exc, "Failed to fetch time entries");
            } finally {
                IsLoading = false;
                EndUpdate ();
            }
        }

        public IEnumerable<DateGroup> DateGroups
        {
            get { return dateGroups; }
        }

        public IEnumerable<object> Data
        {
            get {
                foreach (var grp in dateGroups) {
                    yield return grp;
                    foreach (var data in grp.DataObjects) {
                        yield return data;
                    }
                }
            }
        }

        public int Count
        {
            get {
                var itemsCount = dateGroups.Sum (g => g.DataObjects.Count);
                return dateGroups.Count + itemsCount;
            }
        }

        public event EventHandler OnHasMoreChanged;

        public bool HasMore
        {
            get {
                return hasMore;
            }
            private set {

                if (hasMore == value) {
                    return;
                }

                hasMore = value;

                if (OnHasMoreChanged != null) {
                    OnHasMoreChanged (this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler OnIsLoadingChanged;

        public bool IsLoading
        {
            get {
                return isLoading;
            }
            private set {

                if (isLoading  == value) {
                    return;
                }

                isLoading = value;

                if (OnIsLoadingChanged != null) {
                    OnIsLoadingChanged (this, EventArgs.Empty);
                }
            }
        }

        public class DateGroup
        {
            private readonly DateTime date;
            private readonly List<TimeEntryGroup> dataObjects = new List<TimeEntryGroup>();

            public DateGroup (DateTime date)
            {
                this.date = date.Date;
            }

            public DateTime Date
            {
                get { return date; }
            }

            public List<TimeEntryGroup> DataObjects
            {
                get {
                    return dataObjects;
                }
            }

            public event EventHandler Updated;

            private void OnUpdated ()
            {
                var handler = Updated;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            }

            public void Add (TimeEntryGroup entryGroup)
            {
                dataObjects.Add (entryGroup);
                OnUpdated();
            }

            public void Update (TimeEntryGroup entryGroup)
            {
                for (int i = 0; i < dataObjects.Count; i++) {
                    if (dataObjects[i].Model.Data.Matches (entryGroup.Model.Data)) {
                        dataObjects [i] = entryGroup;
                    }
                }
            }

            public void Remove (TimeEntryGroup entryGroup)
            {
                entryGroup.Dispose();
                dataObjects.Remove (entryGroup);
                OnUpdated();
            }

            public void Sort ()
            {
                foreach (var item in dataObjects) {
                    item.Sort();
                }
                dataObjects.Sort ((a, b) => b.LastStartTime.CompareTo (a.LastStartTime));
                OnUpdated ();
            }
        }

        private enum UpdateMode {
            Immediate,
            Batch,
        }
    }
}
