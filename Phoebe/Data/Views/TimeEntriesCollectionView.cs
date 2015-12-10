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
    public class TimeEntriesCollectionView : ICollectionDataView<IHolder>
    {
        #region Fields
        public static int MaxInitLocalEntries = 20;
        public static int UndoSecondsInterval = 5;
        public static int BufferMilliseconds = 500;

        protected string Tag = "TimeEntriesCollectionView";
        protected ITimeEntryHolder LastRemovedItem;

        private event EventHandler<TimeEntryAction> Update;

        public readonly ObservableRangeCollection<IHolder> Items = new ObservableRangeCollection<IHolder> ();
        private readonly CancellationTokenSource cts = new CancellationTokenSource ();
        private readonly Action disposeSubscription;
        private readonly bool isGrouped;

        private DateTime startFrom = Time.UtcNow;
        private System.Timers.Timer undoTimer;
        #endregion

        #region Life Cycle
        public TimeEntriesCollectionView (bool isGrouped)
        {
            this.isGrouped = isGrouped;
            HasMore = true;

            // The code modifying the collection is responsible to do it in UI thread
            Items.CollectionChanged += (sender, e) => {
                if (CollectionChanged != null) {
                    CollectionChanged (this, e);
                }
            };

            // Subscribe to MessageBus
            var bus = ServiceContainer.Resolve<MessageBus> ();
            var subscription = bus.Subscribe<DataChangeMessage> (UpdateFromMessageBus);
            disposeSubscription = () => bus.Unsubscribe (subscription);

            Observable.FromEventPattern<TimeEntryAction> (
                handler => Update += handler,
                handler => Update -= handler
            )
            .Select (ev => ev.EventArgs)
            .TimedBuffer (BufferMilliseconds)
            // SelectMany would process tasks in parallel, see https://goo.gl/eayv5N
            .Select (msgs => Observable.FromAsync (() => UpdateAsync (msgs))) // TODO: Would ConfigureAwait (false) have any benefit here?
            .Concat() // TODO: Necessary?
            .Subscribe();

            Update (this, TimeEntryAction.Load.New (isInitial: true));
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

            if (disposeSubscription != null) {
                disposeSubscription();
            }
        }
        #endregion

        #region ICollectionDataView implementation
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void LoadMore ()
        {
            if (HasMore)
                Update (this, TimeEntryAction.Load.New ());
        }

        public IEnumerable<IHolder> Data
        {
            get { return Items; }
        }

        public int Count
        {
            get { return Items.Count; }
        }

        public bool HasMore
        {
            get;
            private set;
        }
        #endregion

        #region Utility
        private IList<IHolder> CreateItemCollection (IEnumerable<ITimeEntryHolder> timeHolders)
        {
            return timeHolders
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
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

        private int FindIndex (IList<ITimeEntryHolder> timeHolders, TimeEntryData entry)
        {
            var foundIndex = -1;
            for (var j = 0; j < timeHolders.Count; j++) {
                if (timeHolders [j].Matches (entry)) {
                    foundIndex = j;
                    break;
                }
            }
            return foundIndex;
        }

        private void UpdateFromMessageBus (DataChangeMessage msg)
        {
            var entry = msg != null ? msg.Data as TimeEntryData : null;
            if (entry != null && Update != null) {
                var isExcluded = entry.DeletedAt != null
                                 || msg.Action == DataAction.Delete
                                 || entry.State == TimeEntryState.New;
                Update (this, isExcluded ? TimeEntryAction.Delete.New (entry) : TimeEntryAction.Put.New (entry));
            }
        }
        #endregion

        #region Update Items
        private Task UpdateAsync (TimeEntryData entry, bool delete = false)
        {
            var li = new List<TimeEntryAction> ();
            li.Add (delete ? TimeEntryAction.Delete.New (entry) : TimeEntryAction.Put.New (entry));
            return UpdateAsync (li);
        }

        private async Task UpdateAsync (IEnumerable<TimeEntryAction> actions)
        {
            try {
                // 1. Get only TimeEntryHolders from current collection
                var timeHolders = Items.OfType<ITimeEntryHolder> ().ToList ();

                // 2. Remove, replace or add items from messages
                // TODO: Is it more performant to run CreateTimeEntryHolder tasks in parallel?
                foreach (var action in actions) {
                    await UpdateTimeHoldersAsync (timeHolders, action);
                }

                // 3. Sort new list
                timeHolders = timeHolders.OrderByDescending (x => x.GetStartTime ()).ToList ();

                // 4. Create the new item collection from holders (add headers...)
                var newItemCollection = CreateItemCollection (timeHolders);

                // 5. Check diffs, modify ItemCollection and notify changes
                var diffs = Diff.CalculateExtra (Items, newItemCollection).ToList ();

                // CollectionChanged events must be fired on UI thread
                ServiceContainer.Resolve<IPlatformUtils>().DispatchOnUIThread (() => {
                    foreach (var diff in diffs.OrderBy (x => x.NewIndex).ThenBy (x => x.OldIndex)) {
                        switch (diff.Type) {
                        case DiffType.Add:
                            Items.Insert (diff.NewIndex, diff.NewItem);
                            break;
                        case DiffType.Remove:
                            Items.RemoveAt (diff.NewIndex);
                            break;
                        case DiffType.Move:
                            var oldIndex = Items.IndexOf (diff.OldItem);
                            Items.Move (oldIndex, diff.NewIndex, diff.NewItem);
                            break;
                        case DiffType.Replace:
                            Items[diff.NewIndex] = diff.NewItem;
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

        private async Task UpdateTimeHoldersAsync (
            IList<ITimeEntryHolder> timeHolders, TimeEntryAction action)
        {
            await action.Match (
                put: async x => {
                    var foundIndex = FindIndex (timeHolders, x.Data);
                    if (foundIndex > -1) {
                        timeHolders[foundIndex] = await CreateTimeHolder (x.Data, timeHolders[foundIndex]); // Replace
                    } else {
                        timeHolders.Add (await CreateTimeHolder (x.Data)); // Insert
                    }
                },
                delete: async x => {
                    var foundIndex = FindIndex (timeHolders, x.Data);
                    if (foundIndex > -1) {
                        timeHolders.RemoveAt (foundIndex); // Remove
                    }
                },
                batch: x => BatchUpdateTimeHoldersAsync (timeHolders, x.IsInitial)
           );
        }

        private async Task BatchUpdateTimeHoldersAsync (IList<ITimeEntryHolder> timeHolders, bool initialLoad)
        {
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
                        await UpdateTimeHoldersAsync (timeHolders, TimeEntryAction.Put.New (entry));
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
                    await UpdateTimeHoldersAsync (timeHolders, TimeEntryAction.Put.New (entry));
                }
            }
        }
        #endregion

        #region Time Entry Manipulation
        public async void ContinueTimeEntry (int index)
        {
            // Get data holder
            var timeEntryHolder = Items.ElementAtOrDefault (index) as ITimeEntryHolder;
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
            var timeEntryHolder = Items.ElementAtOrDefault (index) as ITimeEntryHolder;
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
            await UpdateAsync (timeEntryHolder.Data, delete: true);

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
                await UpdateAsync (LastRemovedItem.Data);
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
        public class DateHolder : IHolder
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
        #endregion
    }
}
