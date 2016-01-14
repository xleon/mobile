using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static partial class Store
    {
        static readonly IObservable<StoreResultUntyped> observable;

//        subject.Synchronize (Scheduler.Default) // TODO: Scheduler.CurrentThread for unit tests
//            .Choose (msg => {
//                var cb = ActionRegister.GetCallback (msg.Tag);
//                return cb != null ? Tuple.Create (cb, msg) : null;
//            })
//            .SelectAsync (async tup => tup.Item1 (tup.Item2))
//            .Catch<DataMsgUntyped, Exception> (ex => {
//                Util.LogError ("DISPATCHER", ex);
//                return Observable.Return (DataMsgUntyped.Error (DataTag.UncaughtError, ex.Message));
//            })
//            .Where (msg => msg.Tag != DataTag.UncaughtError);


        static Store ()
        {
            var UNCAUGTH_ERROR = Enum.GetName (typeof (DataTag), DataTag.UncaughtError);

            // Messages are already scheduled in Dispatcher
            observable = Dispatcher.Observe ()
                .Select (msg => {
                    var cb = GetCallback (msg.Tag);
                    if (cb == null) {
                        throw new Exception ("Cannot find Store action for tag: " +
                            Enum.GetName (typeof(DataTag), msg.Tag));
                    }
                    return Tuple.Create (cb, msg);
                })
                .SelectAsync (async tup => await tup.Item1 (tup.Item2))
                .Catch<StoreResultUntyped, Exception> (ex => {
                    Util.LogError ("STORE", ex, "Uncaught error");
                    return Observable.Return (StoreResultUntyped.Error (UNCAUGTH_ERROR, ex.Message));
                })
                .Where (res => res.Tag != UNCAUGTH_ERROR);
        }

        public static IObservable<StoreResult<T>> Observe<T> ()
        {
            return observable
                   .Where (res => res.Tag == typeof (T).FullName)
                   .Select (res => new StoreResult<T> (res));
        }
    }
}

