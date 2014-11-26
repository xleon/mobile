using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public class TimeCorrectionManager : IDisposable
    {
        private const string LogTag = "TimeCorrectionManager";
        private const int SampleSize = 21;

        private readonly object syncRoot = new Object ();
        private readonly Queue<TimeCorrectionData> sample = new Queue<TimeCorrectionData> (SampleSize + 1);
        private Subscription<TogglHttpResponseMessage> subscriptionHttpResponseMessage;
        private TimeSpan? lastCorrection;

        public TimeCorrectionManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionHttpResponseMessage = bus.Subscribe<TogglHttpResponseMessage> (OnHttpResponse);

            LoadMeasurements ();
        }

        public void Dispose ()
        {
            if (subscriptionHttpResponseMessage != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionHttpResponseMessage);
                subscriptionHttpResponseMessage = null;
            }
        }

        private void OnHttpResponse (TogglHttpResponseMessage msg)
        {
            if (msg.ServerTime == null || msg.Latency == null) {
                return;
            }

            var localTime = DateTime.UtcNow;
            var serverTime = msg.ServerTime.Value + TimeSpan.FromTicks (msg.Latency.Value.Ticks / 2);
            var correction = serverTime - localTime;

            AddMeasurement (new TimeCorrectionData () {
                MeasuredAt = serverTime,
                Correction = correction.Ticks,
            });
        }

        public void AddMeasurement (TimeCorrectionData data)
        {
            lock (syncRoot) {
                sample.Enqueue (data);
                lastCorrection = null;

                while (sample.Count >= SampleSize) {
                    sample.Dequeue ();
                }
            }

            SaveMeasurement (data);
        }

        private async void SaveMeasurement (TimeCorrectionData data)
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            await dataStore.ExecuteInTransactionAsync (ctx => {
                ctx.PurgeDatedTimeCorrections (data.MeasuredAt - TimeSpan.FromDays (1));
                ctx.Put (data);
            }).ConfigureAwait (false);
        }

        private async void LoadMeasurements ()
        {
            try {
                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                var rows = await dataStore.Table<TimeCorrectionData> ()
                           .OrderBy (r => r.MeasuredAt, asc: false)
                           .Take (SampleSize)
                           .QueryAsync ()
                           .ConfigureAwait (false);

                rows.Reverse ();

                lock (syncRoot) {
                    foreach (var measurement in rows) {
                        sample.Enqueue (measurement);
                    }
                    lastCorrection = null;
                }
            } catch (InvalidCastException ex) {
                // For whatever reason an InvalidCastException is thrown in the above code occasionally.
                // As it's not clear where or how, it's better to just log this warning and not let it
                // crash the whole app.
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Warning (LogTag, ex, "Failed to load previous measurements.");
            }
        }

        public TimeSpan Correction
        {
            get {
                lock (syncRoot) {
                    if (lastCorrection.HasValue) {
                        return lastCorrection.Value;
                    }

                    if (sample.Count < 1) {
                        lastCorrection = TimeSpan.Zero;
                    } else {
                        // Get the median correction from the samples
                        var dataset = sample.Select (a => a.Correction).ToList ();
                        dataset.Sort ((a, b) => a.CompareTo (b));

                        int midIdx = dataset.Count / 2;
                        if (dataset.Count % 2 == 0) {
                            lastCorrection = TimeSpan.FromTicks ((dataset [midIdx] + dataset [midIdx - 1]) / 2);
                        } else {
                            lastCorrection = TimeSpan.FromTicks (dataset [midIdx]);
                        }
                    }

                    return lastCorrection.Value;
                }
            }
        }
    }
}
