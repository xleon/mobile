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
    public struct TimeEntryMessage
    {

        public TimeEntryData Data { get; private set; }
        public DataAction Action { get; private set; }

        public TimeEntryMessage(TimeEntryData data, DataAction action)
        {
            Data = data;
            Action = action;
        }
    }

    public class TimeEntriesFeed : IObservable<TimeEntryMessage>, IDisposable
    {
        public const int MaxInitLocalEntries = 100;
        public const int DaysLoad = 5;

        private MessageBus bus;
        private DateTime paginationDate;
        private Subscription<DataChangeMessage> subscription;
        private Subscription<UpdateFinishedMessage> updateSubscription;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Subject<TimeEntryMessage> subject = new Subject<TimeEntryMessage> ();

        public TimeEntriesFeed()
        {
            // Set initial pagination Date to the beginning
            // of the next day. So, we can include all entries
            // created -Today-.
            paginationDate = Time.UtcNow.Date.AddDays(1);

            bus = ServiceContainer.Resolve<MessageBus> ();
            updateSubscription = bus.Subscribe<UpdateFinishedMessage> (OnUpdateFinished);
            subscription = bus.Subscribe<DataChangeMessage> (msg =>
            {
                var entry = msg != null ? msg.Data as TimeEntryData : null;
                if (entry != null)
                {
                    var isExcluded = entry.DeletedAt != null
                                     || msg.Action == DataAction.Delete
                                     || entry.State == TimeEntryState.New;
                    subject.OnNext(new TimeEntryMessage(entry, isExcluded ? DataAction.Delete : DataAction.Put));
                }
            });

        }

        public void Dispose()
        {
            // cancel DB request.
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            // Unsubscribe from MessageBus
            if (bus != null && subscription != null)
            {
                bus.Unsubscribe(subscription);
                if (updateSubscription != null)
                {
                    bus.Unsubscribe(updateSubscription);
                }
                subscription = null;
                updateSubscription = null;
                bus = null;
            }

            if (subject != null)
            {
                subject.Dispose();
                // TODO: Remove nullification
                // to avoid a wrong call. This kind of fixes will be
                // replaced with a Fragment navigation system.
                //subject = null;
            }
        }

        public IDisposable Subscribe(IObserver<TimeEntryMessage> observer)
        {
            return subject.Subscribe(observer);
        }

        public async Task<DateTime> LoadMore()
        {
            var endDate = paginationDate;
            var startDate = await GetDatesByDays(endDate, DaysLoad);

            // Always fall back to local data:
            var store = ServiceContainer.Resolve<IDataStore> ();
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId();
            var baseQuery = store.Table<TimeEntryData> ().Where(r =>
                            r.State != TimeEntryState.New &&
                            r.StartTime >= startDate && r.StartTime < endDate &&
                            r.DeletedAt == null &&
                            r.UserId == userId).Take(MaxInitLocalEntries);

            var entries = await baseQuery.OrderByDescending(r => r.StartTime).ToListAsync();
            entries.ForEach(entry => subject.OnNext(new TimeEntryMessage(entry, DataAction.Put)));
            paginationDate = entries.Count > 0 ? startDate : endDate;

            // Return old paginationDate to get the same data from server
            // using the sync manager.
            return endDate;
        }

        private void OnUpdateFinished(UpdateFinishedMessage msg)
        {
            // If there are no items, there is
            // no way to predict the pagination.
            if (!msg.HadErrors)
            {
                paginationDate = msg.EndDate;
            }
        }

        // TODO: replace this method from the SQLite equivalent.
        private async Task<DateTime> GetDatesByDays(DateTime startDate, int numDays)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var baseQuery = store.Table<TimeEntryData> ().Where(r =>
                            r.State != TimeEntryState.New &&
                            r.StartTime < startDate &&
                            r.DeletedAt == null);

            var entries = await baseQuery.ToListAsync(cts.Token);
            if (entries.Count > 0)
            {
                var group = entries.OrderByDescending(r => r.StartTime).GroupBy(t => t.StartTime.Date).Take(numDays).LastOrDefault();
                return group.Key;
            }
            return DateTime.MinValue;
        }
    }
}
