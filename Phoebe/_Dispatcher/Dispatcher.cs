using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
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
                .Catch<IDataMsg, Exception> (PropagateError)
                .Where (x => x.Tag != DataTag.UncaughtError);
        }

        public static void Send (DataTag tag)
        {
            subject.OnNext (DataMsg.Success<CommonData> (null, DataAction.Put, tag, DataDir.None));
        }

        public static void SendWrapped<T> (DataTag tag, T data)
        {
            var wrapped = new CommonDataWrapper<T> (data);
            subject.OnNext (DataMsg.Success<CommonDataWrapper<T>> (wrapped, DataAction.Put, tag, DataDir.None));
        }

        public static void Send<T> (DataTag tag, T data, DataAction action = DataAction.Put)
            where T : CommonData
        {
            subject.OnNext (DataMsg.Success<T> (data, action, tag, DataDir.None));
        }
        
        public static void Send<T> (DataTag tag, IEnumerable<DataActionMsg<T>> msgs)
            where T : CommonData
        {
            subject.OnNext (DataMsg.Success<T> (msgs.ToList (), tag, DataDir.None));
        }
        public static void SendError<T> (DataTag tag, Exception ex)

            where T : CommonData
        {
            subject.OnNext (DataMsg.Error<T> (ex, tag, DataDir.None));
        }

        public static IObservable<IDataMsg> Observe ()
        {
            return observable;
        }

        public static IObservable<IDataMsg> PropagateError (Exception ex) =>
            Observable.Return (DataMsg.Error<CommonData> (ex, DataTag.UncaughtError, DataDir.None));
    }
}
