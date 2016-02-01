using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public class Dispatcher
    {
        public static Dispatcher Singleton { get; private set; }

        public static void Init ()
        {
            Singleton = new Dispatcher ();
        }

        IObserver<IDataMsg> observer;
        event EventHandler<IDataMsg> notify;

        Dispatcher ()
        {
            var schedulerProvider = ServiceContainer.Resolve<ISchedulerProvider> ();

            Observable.Create<IDataMsg> (obs => {
                observer = obs;
                return () => {
                    throw new Exception ("Subscription to Dispatcher must end with the app");
                }; 
            })
            // TODO: Scheduler.CurrentThread for unit tests
            .Synchronize (schedulerProvider.GetScheduler ())
            .SelectAsync (msg => DispatcherRegister.ResolveAction (msg))
            .Catch<IDataMsg, Exception> (PropagateError)
            .Where (x => x.Tag != DataTag.UncaughtError)
            .Subscribe (msg => notify.SafeInvoke (this, msg));
        }

        public void Send (DataTag tag)
        {
            observer.OnNext (DataMsg.Success<object> (tag, null));
        }

        public void Send<T> (DataTag tag, T data)
        {
            observer.OnNext (DataMsg.Success<T> (tag, data));
        }

        public void SendError<T> (DataTag tag, Exception ex)
        {
            observer.OnNext (DataMsg.Error<T> (tag, ex));
        }

        public IObservable<IDataMsg> Observe ()
        {
            return Observable.FromEventPattern<IDataMsg> (
                h => notify += h,
                h => notify -= h
            )
                .Select (ev => ev.EventArgs);
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