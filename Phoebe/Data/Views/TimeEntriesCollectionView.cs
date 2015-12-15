using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines ICollectionDataView data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class TimeEntriesCollectionView : ICollectionDataView<IHolder>
    {
        #region Fields
        public static int MaxInitLocalEntries = 20;
        public static int UndoSecondsInterval = 5;

        private const string Tag = "TimeEntriesCollectionView";

        private readonly ObservableRangeCollection<IHolder> items = new ObservableRangeCollection<IHolder> ();
        private readonly Subject<DataChangeMessage> observable = new Subject<DataChangeMessage> ();
        private readonly CancellationTokenSource cts = new CancellationTokenSource ();
        private readonly bool isGrouped;
        private readonly IFeed feed;

        private DateTime startFrom = Time.UtcNow;
        private ITimeEntryHolder LastRemovedItem;
        private System.Timers.Timer undoTimer;
        #endregion

        #region Life Cycle
        TimeEntriesCollectionView (bool isGrouped, IFeed feed = null)
        {
            feed = feed ?? new Feed ();
            this.isGrouped = isGrouped;
            this.feed = feed;
            HasMore = true;

            // The code modifying the collection is responsible to do it in UI thread
            items.CollectionChanged += (sender, e) => {
                if (CollectionChanged != null) {
                    CollectionChanged (this, e);
                }
            };

            observable.Synchronize (feed.UseThreadPool ? (IScheduler)Scheduler.Default : Scheduler.CurrentThread)
            .TimedBuffer (feed.BufferMilliseconds)
            // SelectMany would process tasks in parallel, see https://goo.gl/eayv5N
            .Select (msgs => Observable.FromAsync (() => UpdateItemsAsync (msgs)))
            .Concat()
            .Subscribe();

            feed.SubscribeToMessageBus (UpdateFromMessageBus);
        }

        public static async Task<TimeEntriesCollectionView> Init (bool isGrouped)
        {
            var v = new TimeEntriesCollectionView (isGrouped);
            await v.LoadMore (isInit: true);
            return v;
        }

        /// <summary>
        /// Only for testing purposes
        /// </summary>
        public static async Task<TimeEntriesCollectionView> InitAdHoc (
            bool isGrouped, IFeed testFeed, params TimeEntryData[] timeEntries)
        {
            var v = new TimeEntriesCollectionView (isGrouped, testFeed);
            v.HasMore = false;

            if (timeEntries.Length > 0) {
                var holders = new List<ITimeEntryHolder> ();
                foreach (var entry in timeEntries) {
                    // Create a new entry to protect the reference;
                    var protectedEntry = new TimeEntryData (entry);
                    holders.Add (await testFeed.CreateTimeHolder (isGrouped, protectedEntry));
                }
                v.items.Reset (v.CreateItemCollection (holders));
            }

            return v;
        }

        public void Dispose()
        {
            // cancel web request.
            if (cts != null) {
                cts.Cancel ();
                cts.Dispose ();
            }

            // Release Undo timer
            // A recently deleted item will not be
            // removed
            if (undoTimer != null) {
                undoTimer.Elapsed -= OnUndoTimeFinished;
                undoTimer.Close();
            }

            if (feed != null) {
                feed.Dispose ();
            }
        }
        #endregion

        #region ICollectionDataView implementation
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IEnumerable<IHolder> Data
        {
            get { return items; }
        }

        public int Count
        {
            get { return items.Count; }
        }

        public bool HasMore
        {
            get;
            private set;
        }

        public async Task LoadMore (bool isInit = false)
        {
            if (!HasMore) {
                return;
            }

            var endTime = startFrom;
            var useLocal = isInit;

            // Try with latest data from server first:
            if (!useLocal) {
                const int numDays = 5;
                try {
                    var entries = await feed.DownloadTimeEntries (endTime, numDays, cts.Token);
                    var minStart = entries.Min (x => x.StartTime);
                    HasMore = (endTime.Date - minStart.Date).Days > 0;
                } catch (Exception exc) {
                    var log = ServiceContainer.Resolve<ILogger> ();
                    const string msg = "Failed to fetch time entries {1} days up to {0}";

                    if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                        log.Info (Tag, exc, msg, endTime, numDays);
                    } else {
                        log.Warning (Tag, exc, msg, endTime, numDays);
                    }

//                    HasMore = false; // TODO: Check if this should be here
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
                    observable.OnNext (new DataChangeMessage (null, entry, DataAction.Put));
                }
            }
        }
        #endregion

        #region Utility
        private IList<IHolder> CreateItemCollection (IEnumerable<ITimeEntryHolder> timeHolders)
        {
            return timeHolders
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList ();
        }

        private void UpdateFromMessageBus (DataChangeMessage msg)
        {
            var entry = msg != null ? msg.Data as TimeEntryData : null;
            if (entry != null) {
                var isExcluded = entry.DeletedAt != null
                                 || msg.Action == DataAction.Delete
                                 || entry.State == TimeEntryState.New;
                observable.OnNext (new DataChangeMessage (null, entry, isExcluded ? DataAction.Delete : DataAction.Put));
            }
        }
        #endregion

        #region Update Items
        private async Task UpdateItemsAsync (IEnumerable<DataChangeMessage> msgs)
        {
            try {
                // 1. Get only TimeEntryHolders from current collection
                var timeHolders = items.OfType<ITimeEntryHolder> ().ToList ();

                // 2. Remove, replace or add items from messages
                // TODO: Is it more performant to run CreateTimeEntryHolder tasks in parallel?
                foreach (var msg in msgs) {
                    await UpdateTimeHoldersAsync (timeHolders, msg.Data as TimeEntryData, msg.Action);
                }

                // 3. Create the new item collection from holders (sort and add headers...)
                var newItemCollection = CreateItemCollection (timeHolders);

                // 4. Check diffs, modify ItemCollection and notify changes
                var diffs = Diff.CalculateExtra (items, newItemCollection);

                // CollectionChanged events must be fired on UI thread
                ServiceContainer.Resolve<IPlatformUtils>().DispatchOnUIThread (() => {
                    int fwOffset = 0, bwOffset = 0;
                    foreach (var diff in diffs) {
                        switch (diff.Type) {
                        case DiffType.Add:
                            if (!diff.IsMove) {
                                items.Insert (diff.NewIndex + fwOffset, diff.NewItem);
                            } else {
                                if (diff.Move == DiffMove.Forward) {
                                    fwOffset--;
                                }
                                items.Move (diff.OldIndex + fwOffset + (diff.Move == DiffMove.Backward ? bwOffset : 0),
                                            diff.NewIndex + fwOffset,
                                            diff.NewItem);
                            }
                            bwOffset++;
                            break;
                        case DiffType.Remove:
                            if (!diff.IsMove) {
                                items.RemoveAt (diff.NewIndex);
                                bwOffset--;
                            } else if (diff.Move == DiffMove.Forward) {
                                fwOffset++;
                            }
                            break;
                        case DiffType.Replace:
                            items[diff.NewIndex + fwOffset] = diff.NewItem;
                            break;
                        }
                    }
                });
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger>();
                log.Error (Tag, ex, "Failed to update collection");
            }
        }

        private async Task UpdateTimeHoldersAsync (IList<ITimeEntryHolder> timeHolders, TimeEntryData entry, DataAction action)
        {
            var foundIndex = -1;
            for (var j = 0; j < timeHolders.Count; j++) {
                if (timeHolders [j].Matches (entry)) {
                    foundIndex = j;
                    break;
                }
            }

            if (foundIndex > -1) {
                if (action == DataAction.Put) {
                    timeHolders [foundIndex] = await feed.CreateTimeHolder (isGrouped, entry, timeHolders [foundIndex]);    // Replace
                } else {
                    timeHolders.RemoveAt (foundIndex);    // Remove
                }
            } else {
                if (action == DataAction.Put) {
                    timeHolders.Add (await feed.CreateTimeHolder (isGrouped, entry));    // Insert
                }
            }
        }
        #endregion

        #region Time Entry Manipulation
        public async void ContinueTimeEntry (int index)
        {
            // Get data holder
            var timeEntryHolder = items.ElementAtOrDefault (index) as ITimeEntryHolder;
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

        public async Task RemoveItemWithUndoAsync (int index)
        {
            // Get data holder
            var timeEntryHolder = items.ElementAtOrDefault (index) as ITimeEntryHolder;
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
            observable.OnNext (new DataChangeMessage (null, timeEntryHolder.Data, DataAction.Delete));

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

        public void RestoreItemFromUndo()
        {
            if (LastRemovedItem != null) {
                observable.OnNext (new DataChangeMessage (null, LastRemovedItem.Data, DataAction.Put));
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

        #region Nested classes
        public interface IFeed : IDisposable
        {
            int BufferMilliseconds { get; }

            bool UseThreadPool { get; }

            void SubscribeToMessageBus (Action<DataChangeMessage> action);

            Task<IList<TimeEntryData>> DownloadTimeEntries (DateTime endTime, int numDays, CancellationToken ct);

            Task<ITimeEntryHolder> CreateTimeHolder (bool isGrouped, TimeEntryData entry, ITimeEntryHolder previous = null);
        }

        public class Feed : IFeed
        {
            MessageBus bus;
            Subscription<DataChangeMessage> subscription;

            public int BufferMilliseconds
            {
                get { return 500; }
            }

            public bool UseThreadPool
            {
                get { return true; }
            }

            public async Task<ITimeEntryHolder> CreateTimeHolder (
                bool isGrouped, TimeEntryData entry, ITimeEntryHolder previous = null)
            {
                var holder = isGrouped
                             ? (ITimeEntryHolder)new TimeEntryGroup (entry, previous)
                             : new TimeEntryHolder (entry);
                await holder.LoadInfoAsync ();
                return holder;
            }

            public void SubscribeToMessageBus (Action<DataChangeMessage> action)
            {
                bus = ServiceContainer.Resolve<MessageBus> ();
                subscription = bus.Subscribe<DataChangeMessage> (action);
            }

            public async Task<IList<TimeEntryData>> DownloadTimeEntries (DateTime endTime, int numDays, CancellationToken ct)
            {
                // Download new Entries
                var client = ServiceContainer.Resolve<ITogglClient>();
                var jsonEntries = await client.ListTimeEntries (endTime, numDays, ct);

                // Store them in the local data store
                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                var entries = await dataStore.ExecuteInTransactionAsync (ctx =>
                              jsonEntries.Select (json => json.Import (ctx)).ToList ());

                return entries;
            }

            public void Dispose ()
            {
                if (bus != null && subscription != null) {
                    bus.Unsubscribe (subscription);
                    subscription = null;
                    bus = null;
                }
            }
        }
        #endregion
    }
}
