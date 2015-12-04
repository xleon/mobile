using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using System.Collections.ObjectModel;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines ICollectionDataView data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class TimeEntriesCollectionView : ICollectionDataView<object>, IDisposable
    {
        public static int MaxInitLocalEntries = 20;
        public static int UndoSecondsInterval = 5;
        public static int BufferMilliseconds = 500;

        protected string Tag = "TimeEntriesCollectionView";
        protected ITimeEntryHolder LastRemovedItem;

        private ObservableRangeCollection<IDiffComparable> ItemCollection;
        private DateTime startFrom;
        private IDisposable subscription;
        private System.Timers.Timer undoTimer;
        private bool isInitialised;
        private bool hasMore;
        private bool isGrouped;
        private CancellationTokenSource cts;

        public TimeEntriesCollectionView (bool isGroupedMode)
        {
            ItemCollection = new ObservableRangeCollection<IDiffComparable> ();
            // The code modifying the collection is responsible to do it in UI thread
            ItemCollection.CollectionChanged += (sender, e) => {
                if (CollectionChanged != null) {
                    CollectionChanged (this, e);
                }
            };

            cts = new CancellationTokenSource();
            isGrouped = isGroupedMode;
            HasMore = true;

            subscription = Observable.Create<DataChangeMessage> (obs => {
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

        #region Update List
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private IList<IDiffComparable> CreateItemCollection (IEnumerable<ITimeEntryHolder> timeHolders)
        {
            return timeHolders
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IDiffComparable>().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList ();
        }

        private async Task<ITimeEntryHolder> CreateTimeHolder (TimeEntryData entry, ITimeEntryHolder previous = null)
        {
            var holder = isGrouped
                         ? (ITimeEntryHolder)new TimeEntryGroup ()
                         : new TimeEntryHolder ();
            await holder.LoadAsync (entry, previous);
            return holder;
        }

        /// <summary>
        /// The caller only needs to add or delete time holders.
        /// The list will later be sorted and date headers created.
        /// </summary>
        private async Task BatchUpdateAsync (Func<IList<ITimeEntryHolder>, Task> update)
        {
            IsLoading = true;
            var timeHolders = ItemCollection.OfType<ITimeEntryHolder> ().ToList ();
            try {
                await update (timeHolders);
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger>();
                log.Error (Tag, ex, "Failed to fetch time entries");
            } finally {
                var newItemCollection = CreateItemCollection (
                                            timeHolders.OrderByDescending (x => x.GetStartTime ()));
                ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() =>
                        ItemCollection.Reset (newItemCollection));
                IsLoading = false;
            }
        }

        private async Task UpdateTimeHoldersAsync (
            IList<ITimeEntryHolder> timeHolders, TimeEntryData entry, DataAction action)
        {
            var foundIndex = -1;
            // TODO: Use some cache to improve performance of this
            for (var j = 0; j < timeHolders.Count; j++) {
                if (timeHolders[j].Matches (entry)) {
                    foundIndex = j;
                    break;
                }
            }

            if (foundIndex > -1) {
                if (action == DataAction.Delete) {
                    timeHolders.RemoveAt (foundIndex);   // Remove
                } else {
                    timeHolders[foundIndex] = await CreateTimeHolder (entry, timeHolders[foundIndex]);   // Replace
                }
            }
            // If no match is found, insert non-excluded entries
            else if (action == DataAction.Put) {
                timeHolders.Add (await CreateTimeHolder (entry)); // Insert
            }
        }

        private Task UpdateAsync (TimeEntryData entry, DataAction action)
        {
            return UpdateAsync (new List<Tuple<TimeEntryData, DataAction>>() { Tuple.Create (entry, action) });
        }

        private async Task UpdateAsync (IEnumerable<Tuple<TimeEntryData, DataAction>> actions)
        {
            try {
                // 1. Get only TimeEntryHolders from current collection
                var timeHolders = ItemCollection.OfType<ITimeEntryHolder> ().ToList ();

                // 2. Remove, replace or add items from messages
                // TODO: Is it more performant to run CreateTimeEntryHolder tasks in parallel?
                foreach (var action in actions) {
                    await UpdateTimeHoldersAsync (timeHolders, action.Item1, action.Item2);
                }

                // 3. Sort new list
                timeHolders = timeHolders.OrderByDescending (x =>
                              x.GetStartTime ()).ToList ();

                // 4. Create the new item collection from holders (add headers...)
                var newItemCollection = CreateItemCollection (timeHolders);

                // 5. Check diffs, modify ItemCollection and notify changes
                var diffs = Diff.CalculateExtra (ItemCollection, newItemCollection).ToList ();

                // CollectionChanged events must be fired on UI thread
                ServiceContainer.Resolve<IPlatformUtils>().DispatchOnUIThread (() => {
                    foreach (var diff in diffs.OrderBy (x => x.NewIndex).ThenBy (x => x.OldIndex)) {
                        switch (diff.Type) {
                        case DiffType.Add:
                            ItemCollection.Insert (diff.NewIndex, diff.NewItem);
                            break;
                        case DiffType.Remove:
                            ItemCollection.RemoveAt (diff.NewIndex);
                            break;
                        case DiffType.Move:
                            var oldIndex = ItemCollection.IndexOf (diff.OldItem);
                            ItemCollection.Move (oldIndex, diff.NewIndex, diff.NewItem);
                            break;
                        case DiffType.Replace:
                            ItemCollection[diff.NewIndex] = diff.NewItem;
                            break;
//                        case DiffType.Copy: // Do nothing
                        }
                    }
                });
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger>();
                log.Error (Tag, ex, "Failed to update collection");
            }
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

            await BatchUpdateAsync (async timeHolders => {
                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                var endTime = startFrom;
                var useLocal = initialLoad;

                // Try with latest data from server first:
                if (!useLocal) {
                    const int numDays = 5;
                    try {
                        var minStart = endTime;
                        var client = ServiceContainer.Resolve<ITogglClient>();
                        var jsonEntries = await client.ListTimeEntries (endTime, numDays, cts.Token);

                        var entries = await dataStore.ExecuteInTransactionAsync (ctx =>
                                      jsonEntries.Select (json => json.Import (ctx)).ToList ());

                        // Add entries to list:
                        foreach (var entry in entries) {
                            await UpdateTimeHoldersAsync (timeHolders, entry, DataAction.Put);
                            if (entry.StartTime < minStart) {
                                minStart = entry.StartTime;
                            }
                        }

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
                                    .Where (r => r.State != TimeEntryState.New
                                            && r.DeletedAt == null
                                            && r.UserId == userId);

                    var entries = await baseQuery
                                  .Take (MaxInitLocalEntries)
                                  .OrderByDescending (r => r.StartTime)
                                  .ToListAsync ();

                    foreach (var entry in entries) {
                        await UpdateTimeHoldersAsync (timeHolders, entry, DataAction.Put);
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

        public class DateHolder : IDiffComparable
        {
            public DateTime Date { get; }
            public bool IsRunning { get; private set; }
            public TimeSpan TotalDuration { get; private set; }

            public DateHolder (DateTime date, IEnumerable<ITimeEntryHolder> timeHolders)
            {
                var dataObjects = timeHolders.ToList ();
                var totalDuration = TimeSpan.Zero;
                foreach (var item in dataObjects) {
                    totalDuration += item.GetDuration ();
                }

                Date = date;
                TotalDuration = totalDuration;
                IsRunning = dataObjects.Any (g => g.Data.State == TimeEntryState.Running);
            }

            public DiffComparison Compare (IDiffComparable other)
            {
                var other2 = other as DateHolder;
                if (other2 == null || other2.Date != Date) {
                    return DiffComparison.Different;
                } else {
                    var same = other2.TotalDuration == TotalDuration && other2.IsRunning == IsRunning;
                    return same ? DiffComparison.Same : DiffComparison.Updated;
                }
            }
        }
    }
}
