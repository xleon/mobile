﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Data.DataObjects;

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
            return observable.OfType<DataMsg<T>> ();
        }

        public static IObservable<IDataMsg> ObserveTag (DataTag tag)
        {
            return observable.Where (x => x.Tag == tag);
        }
    }
}
