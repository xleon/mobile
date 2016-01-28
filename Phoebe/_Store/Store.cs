using System;
using System.Reactive.Linq;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Helpers;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public class Store
    {
        public static Store Singleton { get; private set; }

        public static void Init ()
        {
            Dispatcher.Init ();
            Singleton = new Store ();
        }

        event EventHandler<IDataMsg> notify;
        readonly IDataStore dataStore = ServiceContainer.Resolve<IDataStore> ();

        Store ()
        {
            // Messages are already scheduled in Dispatcher
            Dispatcher.Singleton
            .Observe ()
            .SelectAsync (msg => StoreRegister.ResolveAction (msg, dataStore))
            .Catch<IDataMsg, Exception> (Dispatcher.PropagateError)
            .Where (x => x.Tag != DataTag.UncaughtError)
            .Subscribe (msg => {
                if (notify != null)
                    notify (this, msg);
            });
        }

        public IObservable<IDataMsg> Observe ()
        {
            return Observable.FromEventPattern<IDataMsg> (
                h => notify += h,
                h => notify -= h
            )
            .Select (ev => ev.EventArgs);
        }

        public IObservable<DataMsg<T>> Observe<T> ()
        {
            return Observable.FromEventPattern<IDataMsg> (
                h => notify += h,
                h => notify -= h
            )
            .Select (ev => ev.EventArgs)
            .OfType<DataMsg<T>> ();
        }
    }
}
