using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static partial class Store
    {
        static readonly IObservable<StoreResultUntyped> observable;
        static readonly Subject<DataMsgUntyped> subject = new Subject<DataMsgUntyped> ();

        static Store ()
        {
            var UNCAUGTH_ERROR = Enum.GetName (typeof (DataTag), DataTag.UncaughtError);

            observable = subject
                         // Messages only come from Dispatcher so scheduling shouldn't be necessary
                         // .Synchronize (Scheduler.Default)
            .Choose (msg => {
                var cb = GetCallback (msg.Tag);
                return cb != null ? Tuple.Create (cb, msg) : null;
            })
            .SelectAsync (tup => Util.TryCatchAsync (
                              () => tup.Item1 (tup.Item2),
            ex => {
                var tag =  Enum.GetName (typeof (DataTag), tup.Item2.Tag);
                Util.LogError ("STORE", ex, "Uncaught error. Original tag: " + tag);
                return StoreResultUntyped.Error (UNCAUGTH_ERROR, ex.Message);
            }))
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

