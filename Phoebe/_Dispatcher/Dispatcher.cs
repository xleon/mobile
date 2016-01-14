using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static class Dispatcher
    {
        static readonly IObservable<DataMsgUntyped> observable;
        static readonly Subject<DataMsgUntyped> subject = new Subject<DataMsgUntyped> ();

        static Dispatcher ()
        {
            // TODO: Scheduler.CurrentThread for unit tests
            observable = subject.Synchronize (Scheduler.Default)
                .Select (msg => {
                    var cb = ActionRegister.GetCallback (msg.Tag);
                    if (cb == null) {
                        throw new Exception ("Cannot find Dispatcher action for tag: " +
                            Enum.GetName (typeof(DataTag), msg.Tag));
                    }
                    return Tuple.Create (cb, msg);
                })
                .SelectAsync (async tup => await tup.Item1 (tup.Item2))
                .Catch<DataMsgUntyped, Exception> (ex => {
                    Util.LogError ("DISPATCHER", ex, "Uncaught error");
                    return Observable.Return (DataMsgUntyped.Error (DataTag.UncaughtError, ex.Message));
                })
                .Where (msg => msg.Tag != DataTag.UncaughtError);
        }

        public static void Send (DataTag tag, object data = null)
        {
            subject.OnNext (DataMsgUntyped.Success (tag, data));
        }

        public static IObservable<DataMsgUntyped> Observe ()
        {
            return observable;
        }
    }
}
