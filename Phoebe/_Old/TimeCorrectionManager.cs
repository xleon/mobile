using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe
{
    // TODO RX restore services correctly.
    public class TimeCorrectionManager : IDisposable
    {
        private const string LogTag = "TimeCorrectionManager";
        private const int SampleSize = 21;

        private readonly object syncRoot = new Object ();
        private readonly Queue<TimeCorrectionData> sample = new Queue<TimeCorrectionData> (SampleSize + 1);
        //private Subscription<TogglHttpResponseMessage> subscriptionHttpResponseMessage;
        private TimeSpan? lastCorrection;

        public TimeCorrectionManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            //subscriptionHttpResponseMessage = bus.Subscribe<TogglHttpResponseMessage> (OnHttpResponse);
        }

        public void Dispose ()
        {
            /*
            if (subscriptionHttpResponseMessage != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionHttpResponseMessage);
                subscriptionHttpResponseMessage = null;
            }
            */
        }

        /*
        private void OnHttpResponse (TogglHttpResponseMessage msg)
        {
            if (msg.ServerTime == null || msg.Latency == null) {
                return;
            }

            var localTime = DateTime.UtcNow;
            var serverTime = msg.ServerTime.Value + TimeSpan.FromTicks (msg.Latency.Value.Ticks / 2);
            var correction = serverTime - localTime;

            AddMeasurement (new TimeCorrectionData {
                MeasuredAt = serverTime,
                Correction = correction.Ticks,
            });
        }
        */

        public void AddMeasurement (TimeCorrectionData data)
        {
            lock (syncRoot) {
                sample.Enqueue (data);
                lastCorrection = null;

                while (sample.Count >= SampleSize) {
                    sample.Dequeue ();
                }
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