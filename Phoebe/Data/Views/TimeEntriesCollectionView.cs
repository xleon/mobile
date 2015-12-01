using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Reactive;
using System.Reactive.Linq;
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
    public abstract class TimeEntriesCollectionView : ICollectionDataView<object>, IDisposable
    {
        public static int UndoSecondsInterval = 5;
        public static int BufferMilliseconds = 500;

        protected string Tag = "TimeEntriesCollectionView";
        protected ITimeEntryHolder LastRemovedItem;

        private IList<IHolder> ItemCollection = new List<IHolder> ();
        private DateTime startFrom;
        private IDisposable subscription;
        private System.Timers.Timer undoTimer;
        private bool isInitialised;
        private bool hasMore;
        private CancellationTokenSource cts;

        protected TimeEntriesCollectionView()
        {
            HasMore = true;
            cts = new CancellationTokenSource();
            subscription = Observable.Create<DataChangeMessage> (
            obs => {
                var bus = ServiceContainer.Resolve<MessageBus>();
                var subs = bus.Subscribe<DataChangeMessage> (obs.OnNext);
                return () => bus.Unsubscribe (subs);
            })
            .Where (msg => msg != null && msg.Data != null && msg.Data is TimeEntryData)
            .TimedBuffer (BufferMilliseconds)
            // SelectMany would process tasks in parallel, see https://goo.gl/eayv5N
            .Select (msgs => Observable.FromAsync (() => UpdateAsync (msgs.Select (msg => {
                var entry = msg.Data as TimeEntryData;
                var isExcluded = entry.DeletedAt != null
                                 || msg.Action == DataAction.Delete
                                 || entry.State == TimeEntryState.New;
                return Tuple.Create (entry, isExcluded ? DataAction.Delete : DataAction.Put);
            }))))
            .Concat()
            .Subscribe();
        }

        public void Dispose()
        {
            // cancel web request.
            if (IsLoading) {
                cts.Cancel();
            }
            cts.Dispose();

            // Release Undo timer
            // A recently deleted item will not be
            // removed
            if (undoTimer != null) {
                undoTimer.Elapsed -= OnUndoTimeFinished;
                undoTimer.Close();
            }

            if (subscription != null) {
                subscription.Dispose();
                subscription = null;
            }
        }

        #region Abstract Methods
        protected abstract IList<IHolder> CreateItemCollection (IList<ITimeEntryHolder> timeHolders);
        protected abstract Task<ITimeEntryHolder> CreateTimeHolder (TimeEntryData entry, ITimeEntryHolder previousHolder = null);
        #endregion

        #region Update List
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private Task<bool> UpdateAsync (TimeEntryData entry, DataAction action)
        {
            return UpdateAsync (new List<Tuple<TimeEntryData, DataAction>>() { Tuple.Create (entry, action) });
        }

        /// <summary>
        /// The caller only needs to add or delete time holders.
        /// The list will later be sorted and date headers created.
        /// </summary>
        private async Task<bool> BatchUpdateAsync (Func<IList<ITimeEntryHolder>, Task> update)
        {
            var timeHolders = ItemCollection.OfType<ITimeEntryHolder> ().ToList ();
            try {
                await update (timeHolders);
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger>();
                log.Error (Tag, ex, "Failed to fetch time entries");
                return false;
            } finally {
                // Sort & update ItemCollection
                ItemCollection = CreateItemCollection (timeHolders.OrderByDescending (x =>
                                                       x.GetStartTime ()).ToList ());
                if (CollectionChanged != null) {
                    ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() =>
                            CollectionChanged (this, new NotifyCollectionChangedEventArgs (
                                                   NotifyCollectionChangedAction.Reset)));
                }
            }
            return true;
        }

        private async Task<bool> UpdateAsync (IEnumerable<Tuple<TimeEntryData, DataAction>> actions)
        {
            try {
                // 1. Get only TimeEntryHolders from current collection
                var timeHolders = ItemCollection.OfType<ITimeEntryHolder> ().ToList ();

                // 2. Remove, replace or add items from messages
                // TODO: Use some cache to improve performance of GetTimeEntryHolderIndex
                // TODO: Is it more performant to run CreateTimeEntryHolder tasks in parallel?
                foreach (var action in actions) {
                    var i = -1;
                    var entry = action.Item1;
                    for (var j = 0; j < timeHolders.Count; j++) {
                        if (timeHolders[j].Matches (entry)) {
                            i = j;
                            break;
                        }
                    }

                    if (i > -1) {
                        if (action.Item2 == DataAction.Delete) {
                            timeHolders.RemoveAt (i);   // Remove
                        } else {
                            timeHolders[i] = await CreateTimeHolder (entry, timeHolders[i]);   // Replace
                        }
                    }
                    // If no match is found, insert non-excluded entries
                    else if (action.Item2 == DataAction.Put) {
                        timeHolders.Add (await CreateTimeHolder (entry)); // Insert
                    }
                }

                // 3. Sort new list
                timeHolders = timeHolders.OrderByDescending (x =>
                              x.GetStartTime ()).ToList ();

                // 4. Create the new item collection from holders (add headers...)
                var newItemCollection = CreateItemCollection (timeHolders);

                // 5. Check diffs, modify ItemCollection and notify changes
                var diffs = Diff.Calculate (ItemCollection, newItemCollection)
                            .OrderBy (diff => diff.OldIndex)
                            .ToList();

                // CollectionChanged events must be fired on UI thread
                ServiceContainer.Resolve<IPlatformUtils>().DispatchOnUIThread (() => {
                    var offset = 0;
                    diffs.Select (diff => {
                        System.Diagnostics.Debug.WriteLine (diff);
                        var newItem = newItemCollection.ElementAtOrDefault (diff.NewIndex);
                        var oldItem = ItemCollection.ElementAtOrDefault (diff.OldIndex + offset);
                        switch (diff.Type) {
                        case DiffSectionType.Add:
                            ItemCollection.Insert (diff.OldIndex + offset, newItem);
                            return new NotifyCollectionChangedEventArgs (
                                       NotifyCollectionChangedAction.Add, newItem, diff.OldIndex + offset++);
                        case DiffSectionType.Remove:
                            ItemCollection.RemoveAt (diff.OldIndex + offset);
                            return new NotifyCollectionChangedEventArgs (
                                       NotifyCollectionChangedAction.Remove, oldItem, diff.OldIndex + offset--);

                        case DiffSectionType.Copy:
                        default:
                            // TODO: Check if this is Move action instead
                            var isUpdated = ! (newItem is DateHolder) && !object.ReferenceEquals (oldItem, newItem);
                            ItemCollection[diff.OldIndex + offset] = newItem;
                            return isUpdated
                                   ? new NotifyCollectionChangedEventArgs (
                                       NotifyCollectionChangedAction.Replace, newItem, oldItem, diff.OldIndex + offset)
                                   : null;
                        }
                    })
                    .ForEach (arg => {
                        if (arg != null && CollectionChanged != null) {
                            CollectionChanged (this, arg);
                        }
                    });
                });
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger>();
                log.Error (Tag, ex, "Failed to update collection");
                return false;
            }
            return true;
        }
        #endregion

        #region TimeEntry operations
        public async void ContinueTimeEntry (int index)
        {
            // Get data holder
            var timeEntryHolder = ItemCollection.ElementAtOrDefault (index) as ITimeEntryHolder;
            if (timeEntryHolder == null) {
                return;
            }

            if (timeEntryHolder.Data.State == TimeEntryState.Running) {
                await TimeEntryModel.StopAsync (timeEntryHolder.Data);
                ServiceContainer.Resolve<ITracker>().SendTimerStopEvent (TimerStopSource.App);
            } else {
                await TimeEntryModel.ContinueTimeEntryDataAsync (timeEntryHolder.Data);
                ServiceContainer.Resolve<ITracker>().SendTimerStartEvent (TimerStartSource.AppContinue);
            }
        }
        #endregion

        #region Undo
        public async Task RemoveItemWithUndoAsync (int index)
        {
            // Get data holder
            var timeEntryHolder = ItemCollection.ElementAtOrDefault (index) as ITimeEntryHolder;
            if (timeEntryHolder == null) {
                return;
            }

            // Remove previous if exists
            if (LastRemovedItem != null) {
                await RemoveItemPermanentlyAsync (LastRemovedItem);
            }

            if (timeEntryHolder.Data.State == TimeEntryState.Running) {
                await TimeEntryModel.StopAsync (timeEntryHolder.Data);
            }
            LastRemovedItem = timeEntryHolder;

            // Remove item only from list
            await UpdateAsync (timeEntryHolder.Data, DataAction.Delete);

            // Create Undo timer
            if (undoTimer != null) {
                undoTimer.Elapsed -= OnUndoTimeFinished;
                undoTimer.Close();
            }
            // Using the correct timer.
            undoTimer = new System.Timers.Timer ((UndoSecondsInterval + 1) * 1000);
            undoTimer.AutoReset = false;
            undoTimer.Elapsed += OnUndoTimeFinished;
            undoTimer.Start();
        }

        public async Task RestoreItemFromUndoAsync()
        {
            if (LastRemovedItem != null) {
                await UpdateAsync (LastRemovedItem.Data, DataAction.Put);
                LastRemovedItem = null;
            }
        }

        private async Task RemoveItemPermanentlyAsync (ITimeEntryHolder holder)
        {
            if (holder != null) {
                await holder.DeleteAsync ();
            }
        }

        private async void OnUndoTimeFinished (object sender, ElapsedEventArgs e)
        {
            await RemoveItemPermanentlyAsync (LastRemovedItem);
            LastRemovedItem = null;
        }
        #endregion

        #region Load
        public async Task ReloadAsync()
        {
            if (IsLoading) {
                return;
            }

            // Temporal hack to be sure that
            // collection is initialised.
            isInitialised = true;

            startFrom = Time.UtcNow;
            HasMore = true;

            await LoadAsync (true);
        }

        public async Task LoadMoreAsync()
        {
            if (isInitialised) {
                await LoadAsync (false);
            }
        }

        private async Task LoadAsync (bool initialLoad)
        {
            if (IsLoading || !HasMore) {
                return;
            }

            IsLoading = true;
            var client = ServiceContainer.Resolve<ITogglClient>();

            await BatchUpdateAsync (async timeHolders => {
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
                        var jsonEntries = await client.ListTimeEntries (endTime, numDays, cts.Token);

                        var entries = await dataStore.ExecuteInTransactionAsync (ctx =>
                                      jsonEntries.Select (json => json.Import (ctx)).ToList ());

                        // Add entries to list:
                        foreach (var entry in entries) {
                            timeHolders.Add (await CreateTimeHolder (entry));
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
                                    .OrderByDescending (r => r.StartTime)
                                    .Where (r => r.State != TimeEntryState.New
                                            && r.DeletedAt == null
                                            && r.UserId == userId).Take (20);
                    //var entries = await baseQuery.ToListAsync (r => r.StartTime <= endTime && r.StartTime > startTime);
                    var entries = await baseQuery.ToListAsync ();

                    foreach (var entry in entries) {
                        timeHolders.Add (await CreateTimeHolder (entry));
                    }

                    if (!initialLoad) {
                        var count = await baseQuery
                                    .Where (r => r.StartTime <= startTime)
                                    .CountAsync ();
                        HasMore = count > 0;
                    }
                }
            });
        }

        public event EventHandler HasMoreChanged;

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

                if (HasMoreChanged != null) {
                    HasMoreChanged (this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler IsLoadingChanged;

        private bool isLoading;
        public bool IsLoading
        {
            get {
                return isLoading;
            }
            private set {
                if (isLoading != value) {
                    isLoading = value;
                    if (IsLoadingChanged != null) {
                        IsLoadingChanged (this, EventArgs.Empty);
                    }
                }
            }
        }

        public IEnumerable<object> Data
        {
            get {
                return ItemCollection.Cast<object>();
            }
        }

        public int Count
        {
            get {
                return ItemCollection.Count;
            }
        }
        #endregion

        public class DateHolder : IHolder
        {
            public DateTime Date { get; }
            public IList<ITimeEntryHolder> DataObjects { get; private set; }

            public bool IsRunning
            {
                get { return DataObjects.Any (g => g.Data.State == TimeEntryState.Running); }
            }

            public TimeSpan TotalDuration
            {
                get {
                    TimeSpan totalDuration = TimeSpan.Zero;
                    foreach (var item in DataObjects) {
                        totalDuration += item.GetDuration ();
                    }
                    return totalDuration;
                }
            }

            public DateHolder (DateTime date, IEnumerable<ITimeEntryHolder> timeHolders)
            {
                Date = date;
                DataObjects = timeHolders.ToList ();
            }

            public bool Equals (IHolder other)
            {
                var other2 = other as DateHolder;
                return other2 != null && other2.Date == Date;
            }
        }
    }
}
