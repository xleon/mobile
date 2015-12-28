using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static class Store
    {
        static readonly Subject<ActionMsg> subject = new Subject<ActionMsg> ();
        static readonly ConcurrentDictionary<string, IList<Action<object>>> suscriptors =
            new ConcurrentDictionary<string, IList<Action<object>>> ();

        static Store ()
        {
            subject
            // Messages only come from Dispatcher so scheduling shouldn't be necessary
            // .Synchronize (Scheduler.Default)
            .Choose (msg => {
                var cb = GetCallback (msg.Tag);
                return cb != null ? Tuple.Create (msg, cb) : null;
            })
            .SelectAsync (tup => tup.Item1.TryProcessInStore (tup.Item2))
            .Where (msg => msg.Tag != ActionMsg.ERROR)
            .Subscribe (msg => {
                IList<Action<object>> cbs = null;
                var success = suscriptors.TryGetValue (msg.Tag, out cbs);
                if (success) {
                    foreach (var cb in cbs) {
                        cb (msg.Data);
                    }
                }
            });
        }

        /// <summary>
        /// Only Dispatcher must call this method
        /// </summary>
        public static void Send (ActionMsg msg)
        {
            subject.OnNext (msg);
        }

        public static void Subscribe (string tag, Action<object> callback)
        {
            suscriptors.AddOrUpdate (tag,
            _ => new List<Action<object>> { callback },
            (_, cbs) => {
                cbs.Add (callback);
                return cbs;
            });
        }

        public static bool Unsubscribe (string tag, Action<object> callback)
        {
            IList<Action<object>> cbs = null;
            var success = suscriptors.TryGetValue (tag, out cbs);
            if (success) {
                var cbs2 = cbs.Where (x => x != callback).ToList ();
                success = cbs.Count > cbs2.Count;
                if (success) {
                    success = suscriptors.TryUpdate (tag, cbs2, cbs);
                }
            }
            return success;
        }

        public static Func<object, Task<ActionMsg>> GetCallback (string tag)
        {
            switch (tag) {
            default:
                return null;
            }
        }
    }
}

