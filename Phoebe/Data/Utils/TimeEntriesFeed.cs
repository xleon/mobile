using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    public struct TimeEntryMessage {
        public TimeEntryData Data { get; private set; }
        public DataAction Action { get; private set; }

        public TimeEntryMessage (TimeEntryData data, DataAction action)
        {
            Data = data;
            Action = action;
        }
    }

    public class TimeEntriesFeed : IObservable<TimeEntryMessage>, IDisposable
    {
        public const int MaxInitLocalEntries = 20;
        public const int UndoSecondsInterval = 5;
        public const int DaysServerLoad = 4;
        public const int DaysLocalLoad = 9;

        private MessageBus bus;
        private DateTime startFrom = Time.UtcNow;
        private ITimeEntryHolder lastRemovedItem;
        private Subscription<DataChangeMessage> subscription;
        private System.Timers.Timer undoTimer = new System.Timers.Timer ();
        private CancellationTokenSource cts = new CancellationTokenSource ();
        private Subject<TimeEntryMessage> subject = new Subject<TimeEntryMessage> ();

        public bool HasMore { get; private set; } = true;

        public TimeEntriesFeed ()
        {
            bus = ServiceContainer.Resolve<MessageBus> ();
            subscription = bus.Subscribe<DataChangeMessage> (msg => {
                var entry = msg != null ? msg.Data as TimeEntryData : null;
                if (entry != null) {
                    var isExcluded = entry.DeletedAt != null
                                     || msg.Action == DataAction.Delete
                                     || entry.State == TimeEntryState.New;
                    subject.OnNext (new TimeEntryMessage (entry, isExcluded ? DataAction.Delete : DataAction.Put));
                }
            });
        }

        public void Dispose ()
        {
            if (subject != null) {
                subject.Dispose ();
                subject = null;
            }

            // cancel web request.
            if (cts != null) {
                cts.Cancel ();
                cts.Dispose ();
                cts = null;
            }

            // Release Undo timer
            // A recently deleted item will not be
            // removed
            if (undoTimer != null) {
                undoTimer.Elapsed -= OnUndoTimeFinished;
                undoTimer.Close();
                undoTimer = null;
            }

            // Unsubscribe from MessageBus
            if (bus != null && subscription != null) {
                bus.Unsubscribe (subscription);
                subscription = null;
                bus = null;
            }
        }

        public IDisposable Subscribe (IObserver<TimeEntryMessage> observer)
        {
            return subject.Subscribe (observer);
        }

        public async Task LoadMore (bool isInit = false)
        {
            if (!HasMore) {
                return;
            }

            var endTime = startFrom;
            var useLocal = isInit;
            startFrom = endTime - TimeSpan.FromDays (useLocal ? DaysLocalLoad : DaysServerLoad);

            // Try with latest data from server first:
            if (!useLocal) {
                // Add one day to see if more entries can be downloaded
                const int numDays = DaysServerLoad + 1;
                try {
                    // Download new Entries
                    var client = ServiceContainer.Resolve<ITogglClient> ();
                    var jsonEntries = await client.ListTimeEntries (endTime, numDays, cts.Token);

                    // Store them in the local data store
                    var dataStore = ServiceContainer.Resolve<IDataStore> ();
                    var entries = await dataStore.ExecuteInTransactionAsync (ctx =>
                                  jsonEntries.Select (json => json.Import (ctx)).ToList ());

                    var minStart = entries.Min (x => x.StartTime);
                    HasMore = (endTime.Date - minStart.Date).Days > 0;
                } catch (Exception exc) {
                    useLocal = true;
                    var tag = GetType ().Name;
                    var log = ServiceContainer.Resolve<ILogger> ();
                    const string msg = "Failed to fetch time entries {1} days up to {0}";
                    if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                        log.Info (tag, exc, msg, endTime, numDays);
                    } else {
                        log.Warning (tag, exc, msg, endTime, numDays);
                    }
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

                (await baseQuery.Take (MaxInitLocalEntries)
                 .OrderByDescending (r => r.StartTime)
                 .ToListAsync ())
                .ForEach (entry => subject.OnNext (new TimeEntryMessage (entry, DataAction.Put)));

                if (!isInit) {
                    HasMore = (await baseQuery.CountAsync ()) > MaxInitLocalEntries;
                }
            }
        }

        public async Task ContinueTimeEntryAsync (ITimeEntryHolder timeEntryHolder)
        {
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

        public async Task RemoveItemWithUndoAsync (ITimeEntryHolder timeEntryHolder)
        {
            if (timeEntryHolder == null) {
                return;
            }

            // Remove previous if exists
            if (lastRemovedItem != null) {
                await RemoveItemPermanentlyAsync (lastRemovedItem);
            }

            if (timeEntryHolder.Data.State == TimeEntryState.Running) {
                await TimeEntryModel.StopAsync (timeEntryHolder.Data);
            }
            lastRemovedItem = timeEntryHolder;

            // Remove item only from list
            subject.OnNext (new TimeEntryMessage (timeEntryHolder.Data, DataAction.Delete));

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
            if (lastRemovedItem != null) {
                subject.OnNext (new TimeEntryMessage (lastRemovedItem.Data, DataAction.Put));
                lastRemovedItem = null;
            }
        }

        private async Task RemoveItemPermanentlyAsync (ITimeEntryHolder holder)
        {
            if (holder != null) {
                await holder.DeleteAsync ();
            }
        }

        private async void OnUndoTimeFinished (object sender, System.Timers.ElapsedEventArgs e)
        {
            await RemoveItemPermanentlyAsync (lastRemovedItem);
            lastRemovedItem = null;
        }
    }
}
