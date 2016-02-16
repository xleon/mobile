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

        public static void Init (AppState initState, Reducer<AppState> reducer, ISchedulerProvider schedulerProvider)
        {
            Singleton = Singleton ?? new StoreManager (initState, reducer, schedulerProvider);
        }

        public static void Cleanup ()
        {
            Singleton = null;
        }

        readonly Subject<DataMsg> subject1 = new Subject<DataMsg> ();
        readonly Subject<DataSyncMsg<AppState>> subject2 = new Subject<DataSyncMsg<AppState>> ();

        StoreManager (AppState initState, Reducer<AppState> reducer, ISchedulerProvider schedulerProvider)
        {
            var initSyncMsg = DataSyncMsg.Create (initState);

            subject1
            .Synchronize (schedulerProvider.GetScheduler ())
            .Scan (initSyncMsg, (acc, msg) => {
                try {
                    return reducer.Reduce (acc.State, msg);
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<ILogger> ();
                    log.Error (GetType ().Name, ex, "Failed to update state");
                    return DataSyncMsg.Create (acc.State);
                }
            })
            .Subscribe (subject2.OnNext);
        }

        public void Send (DataMsg msg)
        {
            subject1.OnNext (msg);
        }

        public IObservable<DataSyncMsg<AppState>> Observe ()
        {
            return subject2;
        }

        public IObservable<T> Observe<T> (Func<AppState, T> selector)
        {
            return subject2.Select (syncMsg => selector (syncMsg.State));
        }
    }
}
