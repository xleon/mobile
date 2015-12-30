using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static class Dispatcher
    {
        static readonly Subject<DataMsgUntyped> subject = new Subject<DataMsgUntyped> ();

        static Dispatcher ()
        {
            subject
            .Synchronize (Scheduler.Default) // TODO: Scheduler.CurrentThread for unit tests
            .Choose (msg => {
                var cb = ActionRegister.GetCallback (msg.Tag);
                return cb != null ? Tuple.Create (cb, msg) : null;
            })
            .SelectAsync (tup => Util.TryCatchAsync (
                () => tup.Item1 (tup.Item2),
                ex => {
                        Util.LogError ("DISPATCHER", ex, "Uncaught error. Original tag: " + tup.Item1.Tag);
                    return DataMsgUntyped.Error (DataMsg.UNCAUGHT_ERROR, ex.Message);
                }))
            .Where (msg => msg.Tag != DataMsg.UNCAUGHT_ERROR)
            .Subscribe (Store.Send);
        }

        public static void Send (string tag, object data = null)
        {
            subject.OnNext (DataMsgUntyped.Success (tag, data));
        }
    }
}

