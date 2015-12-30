using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Models
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
        public const int MaxInitLocalEntries = 100;
        public const int UndoSecondsInterval = 5;
        public const int DaysLoad = 5;

        private MessageBus bus;
        private DateTime paginationDate = Time.UtcNow;
        private ITimeEntryHolder lastRemovedItem;
        private Subscription<DataChangeMessage> subscription;
        private Subscription<UpdateFinishedMessage> updateSubscription;
        private System.Timers.Timer undoTimer = new System.Timers.Timer ();
        private CancellationTokenSource cts = new CancellationTokenSource ();
        private Subject<TimeEntryMessage> subject = new Subject<TimeEntryMessage> ();

        public TimeEntriesFeed ()
        {
            bus = ServiceContainer.Resolve<MessageBus> ();
            updateSubscription = bus.Subscribe<UpdateFinishedMessage> (OnUpdateFinished);
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
            // cancel DB request.
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

            if (subject != null) {
                subject.Dispose ();
                subject = null;
            }
        }

        public IDisposable Subscribe (IObserver<TimeEntryMessage> observer)
        {
            return subject.Subscribe (observer);
        }

        public async Task<DateTime> LoadMore ()
        {
            var endDate = paginationDate;
            var startDate = await GetDatesByDays (endDate, DaysLoad);

            // Always fall back to local data:
            var store = ServiceContainer.Resolve<IDataStore> ();
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
            var baseQuery = store.Table<TimeEntryData> ().Where (r =>
                            r.State != TimeEntryState.New &&
                            r.StartTime >= startDate && r.StartTime < endDate &&
                            r.DeletedAt == null &&
                            r.UserId == userId).Take (MaxInitLocalEntries);

            var entries = await baseQuery.OrderByDescending (r => r.StartTime).ToListAsync ();
            entries.ForEach (entry => subject.OnNext (new TimeEntryMessage (entry, DataAction.Put)));

            paginationDate = entries.Count > 0 ? startDate : endDate;

            // Return old paginationDate to get the same data from server
            // using the sync manager.
            return endDate;
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

        private void OnUpdateFinished (UpdateFinishedMessage msg)
        {
            // If there are no items, there is
            // no way to predict the pagination.
            if (!msg.HadErrors) {
                paginationDate = msg.EndDate;
            }
        }

        // TODO: replace this method from the SQLite equivalent.
        private async Task<DateTime> GetDatesByDays (DateTime startDate, int numDays)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var baseQuery = store.Table<TimeEntryData> ().Where (r =>
                            r.State != TimeEntryState.New &&
                            r.StartTime < startDate &&
                            r.DeletedAt == null);

            var entries = await baseQuery.ToListAsync (cts.Token);
            if (entries.Count > 0) {
                var group = entries.OrderByDescending (r => r.StartTime).GroupBy (t => t.StartTime.Date).Take (numDays).LastOrDefault ();
                return group.Key;
            }
            return DateTime.MinValue;
        }
    }
}
