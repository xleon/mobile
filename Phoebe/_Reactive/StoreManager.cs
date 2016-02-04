using System;
using System.Reactive.Linq;
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

		IObserver<IDataMsg> observer;
        event EventHandler<IDataMsg> notify;

        readonly Toggl.Phoebe.Data.IDataStore dataStore =
            ServiceContainer.Resolve<Toggl.Phoebe.Data.IDataStore> ();

        StoreManager ()
        {
            var schedulerProvider = ServiceContainer.Resolve<ISchedulerProvider> ();
            Observable.Create<IDataMsg> (obs => {
                observer = obs;
                return () => {
                    throw new Exception ("Subscription must end with the app");
                }; 
            })
            .Synchronize (schedulerProvider.GetScheduler ())
            .SelectAsync (msg => StoreRegister.ResolveAction (msg, dataStore))
            .Catch<IDataMsg, Exception> (RxChain.PropagateError)
            .Where (x => x.Tag != DataTag.UncaughtError)
            .Subscribe (msg => notify.SafeInvoke (this, msg));
        }

        public void Send (IDataMsg msg)
        {
            observer.OnNext (msg);
        }

        public IObservable<IDataMsg> Observe ()
        {
            return Observable.FromEventPattern<IDataMsg> (
                h => notify += h,
                h => notify -= h
            )
            .Select (ev => ev.EventArgs);
        }
    }
}
