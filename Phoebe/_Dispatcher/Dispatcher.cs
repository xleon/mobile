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
    public class Dispatcher
    {
        public static Dispatcher Singleton { get; private set; }

        public static void Init ()
        {
            Singleton = new Dispatcher ();
        }
        
        readonly IObservable<IDataMsg> observable;
        readonly Subject<IDataMsg> subject = new Subject<IDataMsg> ();

        Dispatcher ()
        {
            // TODO: Scheduler.CurrentThread for unit tests
            observable =
                subject
                .Synchronize (Scheduler.Default)
                .SelectAsync (msg => DispatcherRegister.ResolveAction (msg))
                .Catch<IDataMsg, Exception> (PropagateError)
                .Where (x => x.Tag != DataTag.UncaughtError);
        }

        public void Send (DataTag tag)
        {
            subject.OnNext (DataMsg.Success<object> (tag, null));
        }

        public void Send<T> (DataTag tag, T data)
        {
            subject.OnNext (DataMsg.Success<T> (tag, data));
        }

        public void SendError<T> (DataTag tag, Exception ex)
        {
            subject.OnNext (DataMsg.Error<T> (tag, ex));
        }

        public IObservable<IDataMsg> Observe ()
        {
            return observable;
        }

        public static IObservable<IDataMsg> PropagateError (Exception ex) =>
            Observable.Return (DataMsg.Error<object> (DataTag.UncaughtError, ex));
    }

    public class ActionNotFoundException : Exception
    {
        public DataTag Tag { get; private set; }
        public Type Register { get; private set; }

        public ActionNotFoundException (DataTag tag, Type register)
            : base (Enum.GetName (typeof (DataTag), tag) + " not found in " + register.FullName)
        {
            Tag = tag;
            Register = register;
        }
    }
}