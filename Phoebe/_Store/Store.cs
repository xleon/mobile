using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static partial class Store
    {
        static readonly IObservable<IDataMsg> observable;

        static Store ()
        {
            // Messages are already scheduled in Dispatcher
            observable =
                Dispatcher
                .Observe ()
                .Select (msg => Tuple.Create (GetAction (msg.Tag), msg))
                .SelectAsync (async tup => await tup.Item1 (tup.Item2))
                .Catch<IDataMsg, Exception> (Dispatcher.PropagateError)
                .Where (x => x.Tag != DataTag.UncaughtError);
        }

        public static IObservable<DataMsg<T>> Observe<T> ()
        {
            return observable
                   .Where (msg => msg is DataMsg<T>)
                   .Cast<DataMsg<T>> ();
        }
    }
}

