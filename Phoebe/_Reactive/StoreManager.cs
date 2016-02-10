using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Helpers;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public class StoreManager
    {
        public static StoreManager Singleton { get; private set; }

        public static void Init (AppState initState, Reducer<AppState> reducer)
        {
            Singleton = Singleton ?? new StoreManager (initState, reducer);
        }

        readonly Subject<IDataMsg> subject1 = new Subject<IDataMsg> ();
        readonly Subject<DataSyncMsg<AppState>> subject2 = new Subject<DataSyncMsg<AppState>> ();

        StoreManager (AppState initState, Reducer<AppState> reducer)
        {
            var scheduler = ServiceContainer.Resolve<ISchedulerProvider> ().GetScheduler ();
            var initSyncMsg = DataSyncMsg.Create (DataTag.EmptyQueueAndSync, initState);

            subject1
            .Synchronize (scheduler)
            .Scan (initSyncMsg, (acc, msg) => {
                try {
                    return reducer.Reduce (acc.State, msg);
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<ILogger> ();
                    log.Error (GetType ().Name, ex, "Failed to update state");
                    return acc;
                }
            })
            .Subscribe (subject2.OnNext);
        }

        public void Send (IDataMsg msg)
        {
            subject1.OnNext (msg);
        }

        public IObservable<DataSyncMsg<AppState>> Observe ()
        {
            return subject2.AsObservable ();
        }

        public IObservable<T> Observe<T> (Func<AppState, T> selector)
        {
            return subject2.AsObservable ()
                   .Select (syncMsg => selector (syncMsg.State))
                   .DistinctUntilChanged ();
        }
    }
}
