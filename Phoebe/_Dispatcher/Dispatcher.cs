using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static class Dispatcher
    {
        static readonly Subject<ActionMsg> subject = new Subject<ActionMsg> ();

        static Dispatcher ()
        {
            subject
            .Synchronize (Scheduler.Default) // TODO: Scheduler.CurrentThread for unit tests
            .Choose (msg => {
                var cb = ActionRegister.GetCallback (msg.Tag);
                return cb != null ? Tuple.Create (msg, cb) : null;
            })
            .SelectAsync (tup => tup.Item1.TryProcess (tup.Item2))
            .Where (msg => msg.Tag != ActionMsg.ERROR)
            .Subscribe (Store.Send);
        }

        public static void Send (string tag, object data = null)
        {
            subject.OnNext (new ActionMsg (tag, data));
        }
    }
}

