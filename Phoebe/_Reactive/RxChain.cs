using System;
using System.Reactive.Linq;
using Toggl.Phoebe._Data;

namespace Toggl.Phoebe._Reactive
{
    public static class RxChain
    {
        public static void Init ()
        {
            StoreManager.Init ();
            SyncOutManager.Init ();
        }

        public static void Send (DataTag tag) =>
            StoreManager.Singleton.Send (DataMsg.Success<object> (tag, null));

        public static void Send<T> (DataTag tag, T data) =>
            StoreManager.Singleton.Send (DataMsg.Success<object> (tag, data));

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

