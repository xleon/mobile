using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
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

        public static void Cleanup ()
        {
            Singleton = null;
        }

        Toggl.Phoebe.Net.INetworkPresence networkPresence;
        readonly Subject<Tuple<DataMsg,SyncTestOptions>> subject1 = new Subject<Tuple<DataMsg,SyncTestOptions>> ();
        readonly Subject<DataSyncMsg<AppState>> subject2 = new Subject<DataSyncMsg<AppState>> ();

        StoreManager (AppState initState, Reducer<AppState> reducer)
        {
            var initSyncMsg = DataSyncMsg.Create (initState);

            subject1
            .Synchronize (Scheduler.Default)
            .Scan (initSyncMsg, (acc, tuple) => {
                DataSyncMsg<AppState> msg;
                try {
                    msg = reducer.Reduce (acc.State, tuple.Item1);
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<ILogger> ();
                    log.Error (GetType ().Name, ex, "Failed to update state");
                    msg = DataSyncMsg.Create (acc.State);
                }
                return tuple.Item2 == null ? msg : msg.With (tuple.Item2);
            })
            .Subscribe (subject2.OnNext);
        }

        public void Send (DataMsg msg, SyncTestOptions syncTest)
        {
            subject1.OnNext (Tuple.Create (msg, syncTest));
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
