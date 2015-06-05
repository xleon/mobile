using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines ICollectionDataView data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class TimeEntriesCollectionView : ICollectionDataView<object>, IDisposable
    {
        private static readonly string Tag = "TimeEntriesCollectionView";

        protected readonly List<object> ItemCollection = new List<object> ();
        private readonly List<IDateGroup> dateGroups = new List<IDateGroup> ();

        private UpdateMode updateMode = UpdateMode.Batch;
        private DateTime startFrom;
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private List<Task> updateTasks = new List<Task> ();
        private bool reloadScheduled;
        private bool isLoading;
        private bool hasMore;
        private int lastNumberOfItems;

        // for Undo/Restore operations
        private TimeEntryData lastRemovedItem;

        public TimeEntriesCollectionView ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
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

        #region Update List
        protected virtual void AddOrUpdateEntry (TimeEntryData entry)
        {
            // Avoid a removed item (Undoable)
            // been added again.
            if (lastRemovedItem != null && lastRemovedItem.Matches (entry)) {
                return;
            }
        }

        protected virtual void RemoveEntry (TimeEntryData entry)
        {
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void DispatchCollectionEvent (object item, NotifyCollectionChangedEventArgs args)
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }

            updateTasks.Add (UpdateCollection (item, args));
        }

        protected async Task UpdateCollection (object data, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add) {
                if (e.NewItems.Count == 1) {
                    if (data is IDateGroup) {
                        ItemCollection.Insert (e.NewStartingIndex, data);
                    } else {
                        var timeEntryList = GetListOfTimeEntries (data);
                        var newHolder = new TimeEntryHolder (timeEntryList);
                        await newHolder.LoadAsync ();
                        ItemCollection.Insert (e.NewStartingIndex, newHolder);
                    }
                } else {
                    var holderTaskList = new List<Task> ();
                    var currentItems = new List<object> (UpdatedList);

                    if (e.NewStartingIndex == 0) {
                        ItemCollection.Clear ();
                    }

                    for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++) {
                        var item = currentItems [i];
                        if (item is IDateGroup) {
                            ItemCollection.Insert (i, item);
                        } else {
                            var timeEntryList = GetListOfTimeEntries (item);
                            var timeEntryHolder = new TimeEntryHolder (timeEntryList);
                            ItemCollection.Insert (i, timeEntryHolder);
                            holderTaskList.Add (timeEntryHolder.LoadAsync ());
                        }
                    }
                    await Task.WhenAll (holderTaskList);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                var savedItem = ItemCollection [e.OldStartingIndex];
                ItemCollection.RemoveAt (e.OldStartingIndex);
                ItemCollection.Insert (e.NewStartingIndex, savedItem);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                ItemCollection.RemoveAt (e.OldStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                if (data is IDateGroup) {
                    ItemCollection [e.NewStartingIndex] = data;
                } else {
                    var oldHolder = (TimeEntryHolder)ItemCollection.ElementAt (e.NewStartingIndex);
                    var timeEntryList = GetListOfTimeEntries (data);
                    await oldHolder.UpdateAsync (timeEntryList);
                    ItemCollection [e.NewStartingIndex] = oldHolder;
                }
            }

            var handler = CollectionChanged;
            if (handler != null) {
                handler (this, e);
            }
        }

        private List<TimeEntryData> GetListOfTimeEntries (object data)
        {
            var timeEntryList = new List<TimeEntryData> ();

            if (data is TimeEntryData) {
                timeEntryList.Add ((TimeEntryData)data);
            } else if (data is TimeEntryGroup) {
                timeEntryList = ((TimeEntryGroup)data).TimeEntryList;
            }

            return timeEntryList;
        }
        #endregion

        #region TimeEntry operations
        public async void ContinueTimeEntry (TimeEntryData timeEntryData)
        {
            await TimeEntryModel.ContinueTimeEntryDataAsync (timeEntryData);

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }

        public async void StopTimeEntry (TimeEntryData timeEntryData)
        {
            await TimeEntryModel.StopAsync (timeEntryData);

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
        }
        #endregion

        #region Undo
        public void RestoreItemFromUndo ()
        {
            if (lastRemovedItem != null) {
                AddOrUpdateEntry (lastRemovedItem);
                lastRemovedItem = null;
            }
        }

        public async void RemoveItemWithUndo (TimeEntryData data)
        {
            // Remove previous if exists
            RemoveItemPermanently (lastRemovedItem);
            if (data.State == TimeEntryState.Running) {
                await TimeEntryModel.StopAsync (data);
            }
            lastRemovedItem = data;
            RemoveEntry (data);
        }

        public void ConfirmItemRemove ()
        {
            RemoveItemPermanently (lastRemovedItem);
        }

        private async void RemoveItemPermanently (TimeEntryData itemToRemove)
        {
            if (itemToRemove != null) {
                await TimeEntryModel.DeleteAsync (itemToRemove);
            }
        }
        #endregion

        #region Load
        private async void OnDataChange (DataChangeMessage msg)
        {
            var entry = msg.Data as TimeEntryData;
            if (entry == null) {
                return;
            }

            // Wait for last update tasks.
            // in order to execute the whole process (detect, create event,
            // load data  object, dispatch collection event) sequencially.
            // This method will be replaced by Rx code.
            if (updateTasks.Any (e => !e.IsCompleted)) {
                await Task.WhenAll (updateTasks);
                updateTasks.Clear ();
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
            lastNumberOfItems = Count;
            updateMode = UpdateMode.Batch;
        }

        private void EndUpdate ()
        {
            updateMode = UpdateMode.Immediate;
            if (Count > lastNumberOfItems) {
                DispatchCollectionEvent (new object(), CollectionEventBuilder.GetRangeEvent (NotifyCollectionChangedAction.Add, lastNumberOfItems, Count - lastNumberOfItems));
            }
        }

        public void Reload ()
        {
            if (IsLoading) {
                return;
            }

            startFrom = Time.UtcNow;
            DateGroups.Clear ();
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

        public IEnumerable<object> Data
        {
            get {
                return ItemCollection;
            }
        }

        protected virtual IList<IDateGroup> DateGroups
        {
            get { return dateGroups; }
        }

        protected IEnumerable<object> UpdatedList
        {
            get {
                foreach (var grp in DateGroups) {
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
                var itemsCount = DateGroups.Sum (g => g.DataObjects.Count ());
                return DateGroups.Count + itemsCount;
            }
        }

        #endregion

        public interface IDateGroup
        {
            DateTime Date {  get; }

            bool IsRunning { get; }

            TimeSpan TotalDuration { get; }

            IEnumerable<object> DataObjects { get; }
        }

        private enum UpdateMode {
            Immediate,
            Batch,
        }
    }
}

