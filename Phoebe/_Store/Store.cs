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
                    Util.LogError ("STORE", ex, "Uncaught error. Original tag: " + tup.Item1.Tag);
                    return StoreResultUntyped.Error (DataMsg.UNCAUGHT_ERROR, ex.Message);
            }))
            .Where (res => res.Tag != DataMsg.UNCAUGHT_ERROR);
        }

        /// <summary>
        /// Only Dispatcher must call this method
        /// </summary>
        public static void Send (DataMsgUntyped msg)
        {
            subject.OnNext (msg);
        }

        public static IObservable<StoreResult<T>> Observe<T> ()
        {
            return observable
                .Where(res => res.Tag == typeof(T).FullName)
                .Select(res => new StoreResult<T> (res));
        }
    }
}

