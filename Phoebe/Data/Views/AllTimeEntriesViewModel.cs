using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IDataStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class AllTimeEntriesViewModel : IDataView<object>, IDisposable
    {
        private static readonly string Tag = "AllTimeEntriesViewModel";
        private UpdateMode updateMode = UpdateMode.Immediate;
        private DateTime startFrom;
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private bool reloadScheduled;
        private bool isGrouped;
        private readonly ObservableRangeCollection<DataHolder> items = new ObservableRangeCollection<DataHolder>();
        public event EventHandler Updated; // remove it!

        public AllTimeEntriesViewModel ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);

            isGrouped = ServiceContainer.Resolve<ISettingsStore>().GroupedTimeEntries;
            //HasMore = true;
            //Reload ();
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
                RemoveItem (entry);
            } else {
                AddOrUpdateItem (entry);
            }
        }

        private void AddOrUpdateItem (TimeEntryData entry)
        {
            var index = FindExistingEntry (entry);

            if ( index != -1) {
                var existingEntry = items [index].Model.Data;
                if (entry.StartTime != existingEntry.StartTime) {
                    MoveItem (entry, existingEntry);
                } else {
                    UpdateItem (entry);
                }
            } else {
                AddItem (entry);
            }
        }

        private void AddOrUpdateItemRange (IEnumerable<TimeEntryData> entries)
        {
            TimeEntryData existingEntry;
            //List<DataHolder> range = new List<DataHolder>();

            foreach (var entry in entries) {
                var index = FindExistingEntry (entry);
                if ( index != -1) {
                    existingEntry = items [index].Model.Data;
                    if (entry.StartTime != existingEntry.StartTime) {
                        MoveItem (entry, existingEntry);
                    } else {
                        UpdateItem (entry);
                    }
                } else {
                    AddItem (entry);
                }
            }
        }

        private void AddItem (TimeEntryData newTimeEntry)
        {
            var entryDataHolder = new DataHolder (newTimeEntry);
            var position = GetSortedPosition (newTimeEntry.StartTime);
            items.Insert (position, entryDataHolder);

            UpdateDateGroup (newTimeEntry);
        }

        private void MoveItem (TimeEntryData newTimeEntry, TimeEntryData oldTimeEntry)
        {
            var oldIndex = FindExistingEntry (oldTimeEntry);
            var newIndex = GetSortedPosition (newTimeEntry.StartTime);

            items[oldIndex].Model.Data = newTimeEntry;
            items.Move (newIndex, oldIndex);

            UpdateDateGroup (newTimeEntry);
        }

        private void UpdateItem (TimeEntryData timeEntry)
        {
            var index = FindExistingEntry (timeEntry);
            items[index] = new DataHolder (timeEntry);
            UpdateDateGroup (timeEntry);
        }

        private void RemoveItem (TimeEntryData timeEntry)
        {
            var index = FindExistingEntry (timeEntry);

            if ((index == items.Count - 1 || items[index].IsHeader) && items[index - 1].IsHeader)
                // Remove header too
                items.RemoveRange (new List<DataHolder>() {items [index - 1], items [index]});
            else {
                items.RemoveAt (index);
                UpdateDateGroup (timeEntry);
            }
        }

        private void UpdateDateGroup (TimeEntryData timeEntry)
        {
            var date = timeEntry.StartTime.ToLocalTime ().Date;
            var position = GetSortedPosition (date);

            // Get total duration.
            var totalDuration = new TimeSpan();
            for (int i = position + 1; i < items.Count; i++) {
                if (!items[i].IsHeader) {
                    totalDuration += items[i].Model.GetDuration();
                }
            }

            var dateGroupHolder = new DataHolder ( new DateGroup (date, totalDuration));

            if (items[position].IsHeader) {
                items[position] = dateGroupHolder;
            } else {
                items.Insert (position, dateGroupHolder);
            }
        }

        private int FindExistingEntry (TimeEntryData dataObject)
        {
            var index = -1;
            for (int i = 0; i < items.Count; i++)
                if (dataObject.Matches (items[i].Model)) {
                    index = i;
                }
            return index;
        }

        private int GetSortedPosition (DateTime dateTime)
        {
            var pos = 0;

            for (int i = 0; i < items.Count; i++) {
                if (items[i].Date > dateTime) {
                    pos = i;
                }
            }
            return (pos == 0 && items.Count > 1) ? items.Count - 1 : pos;
        }

        private TimeSpan GetGroupDuration (DataHolder dateGroupItem)
        {
            var totalDuration = new TimeSpan();
            var index = items.IndexOf (dateGroupItem);

            for (int i = index + 1; i < items.Count; i++) {
                if (!items[i].IsHeader) {
                    totalDuration += items[i].Model.GetDuration();
                }
            }

            return totalDuration;
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

        private void BeginUpdate ()
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }
            updateMode = UpdateMode.Batch;
        }

        private void EndUpdate ()
        {
            updateMode = UpdateMode.Immediate;
        }

        public void Reload ()
        {
            if (IsLoading) {
                return;
            }

            items.Clear ();
            startFrom = Time.UtcNow;
            HasMore = true;

            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            if (syncManager.IsRunning) {
                // Fake loading until initial sync has finished
                IsLoading = true;

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
                        AddOrUpdateItemRange (entries);

                        foreach (var entry in entries) {
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
                    AddOrUpdateItemRange (entries);

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

        public IEnumerable<object> Data
        {
            get {
                return null;
            }
        }

        public ObservableRangeCollection<DataHolder> Items
        {
            get {
                return items;
            }
        }

        public long Count
        {
            get {
                return items.Count;
            }
        }

        public bool HasMore { get; private set; }

        public bool IsLoading { get; private set; }

        public class DateGroup
        {
            private readonly DateTime date;
            private readonly TimeSpan duration;

            public DateGroup (DateTime date, TimeSpan duration)
            {
                this.date = date.Date;
                this.duration = duration;
            }

            public DateTime Date
            {
                get { return date; }
            }

            public TimeSpan Duration
            {
                get {
                    return duration;
                }
            }
        }

        public class DataHolder
        {

            private readonly bool isHeader;
            private readonly DateGroup dateGroup;
            private readonly TimeEntryModel timeEntryModel;

            public bool IsHeader
            {
                get {
                    return isHeader;
                }
            }

            public DateTime Date
            {
                get {
                    return isHeader ? dateGroup.Date : timeEntryModel.StartTime;
                }
            }

            public TimeSpan Duration
            {
                get {
                    return dateGroup.Duration;
                }
            }

            public TimeEntryModel Model
            {
                get {
                    return timeEntryModel;
                }
            }

            public DateGroup DateGroup
            {
                get {
                    return dateGroup;
                }
            }

            public DataHolder (TimeEntryData timeEntryData)
            {
                isHeader = false;
                timeEntryModel = (TimeEntryModel)timeEntryData;
            }

            public DataHolder (DateGroup dateGroup)
            {
                isHeader = true;
                this.dateGroup = dateGroup;
            }
        }

        private enum UpdateMode {
            Immediate,
            Batch,
        }

        /// <summary>
        /// Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class ObservableRangeCollection<T> : ObservableCollection<T>
        {
            public void AddRange (IEnumerable<T> collection)
            {
                if (collection == null) { throw new ArgumentNullException ("collection"); }

                foreach (var i in collection) { Items.Add (i); }
                OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, collection as List<T>));
            }

            public void RemoveRange (IEnumerable<T> collection)
            {
                if (collection == null) { throw new ArgumentNullException ("collection"); }

                foreach (var i in collection) { Items.Remove (i); }
                OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, collection.ToList()));
            }

            public void Replace (T item)
            {
                ReplaceRange (new T[] { item });
            }

            public void ReplaceRange (IEnumerable<T> collection)
            {
                if (collection == null) { throw new ArgumentNullException ("collection"); }

                Items.Clear();
                foreach (var i in collection) { Items.Add (i); }
                OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
            }

            public ObservableRangeCollection()
            : base() { }

            public ObservableRangeCollection (IEnumerable<T> collection)
            : base (collection) { }
        }

    }
}
