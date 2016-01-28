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

        readonly IObservable<IDataMsg> observable;
        readonly IDataStore dataStore = ServiceContainer.Resolve<IDataStore> ();

        Store ()
        {
            // Messages are already scheduled in Dispatcher
            observable =
                Dispatcher.Singleton
                .Observe ()
                .SelectAsync (msg => StoreRegister.ResolveAction (msg, dataStore))
                .Catch<IDataMsg, Exception> (Dispatcher.PropagateError)
                .Where (x => x.Tag != DataTag.UncaughtError);
        }

        public IObservable<DataMsg<T>> Observe<T> ()
        {
            return observable.OfType<DataMsg<T>> ();
        }

        public IObservable<IDataMsg> ObserveTag (DataTag tag)
        {
            return observable.Where (x => x.Tag == tag);
        }
    }
}
