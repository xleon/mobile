using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Helpers;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public class StoreManager
    {
        public static StoreManager Singleton { get; private set; }

        public static void Init ()
        {
            Singleton = Singleton ?? new StoreManager ();
        }

        readonly Subject<IDataMsg> subject1 = new Subject<IDataMsg> ();
        readonly Subject<IDataMsg> subject2 = new Subject<IDataMsg> ();

        readonly ISyncDataStore dataStore =
            ServiceContainer.Resolve<ISyncDataStore> ();

        StoreManager ()
        {
            var schedulerProvider = ServiceContainer.Resolve<ISchedulerProvider> ();
            subject1
            .Synchronize (schedulerProvider.GetScheduler ())
            .Select (msg => StoreRegister.ResolveAction (msg, dataStore))
            .Catch<IDataMsg, Exception> (RxChain.PropagateError)
            .Where (x => x.Tag != DataTag.UncaughtError)
            .Subscribe (subject2.OnNext);
        }

        public void Send (IDataMsg msg)
        {
            subject1.OnNext (msg);
        }

        public IObservable<IDataMsg> Observe ()
        {
            return subject2.AsObservable ();
        }
    }
}
