using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe
{
    public static class Dispatcher
    {
        static readonly IObservable<IDataMsg> observable;
        static readonly Subject<IDataMsg> subject = new Subject<IDataMsg> ();

        static Dispatcher ()
        {
            // TODO: Scheduler.CurrentThread for unit tests
            observable =
                subject
                .Synchronize (Scheduler.Default)
                .Select (msg => Tuple.Create (DispatcherRegister.GetAction (msg.Tag), msg))
                .SelectAsync (async tup => await tup.Item1 (tup.Item2))
                .Catch<IDataMsg, Exception> (ex => Observable.Return (
                                                 DataMsg.Error<object> (DataTag.UncaughtError, ex)))
                .Where (x => x.Tag != DataTag.UncaughtError);
        }

        public static void Send (DataTag tag, object data = null)
        {
            subject.OnNext (DataMsg.Success (tag, data));
        }

        public static void SendError<T> (DataTag tag, Exception ex)
        {
            subject.OnNext (DataMsg.Error<T> (tag, ex));
        }

        public static IObservable<IDataMsg> Observe ()
        {
            return observable;
        }
    }
}
