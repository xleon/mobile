using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Helpers;

namespace Toggl.Phoebe._Reactive
{
    public static class RxChain
    {
        readonly static Dictionary<DataTag, Type> protectedTags = new Dictionary<DataTag, Type>
        {
            { DataTag.TestSyncOutManager, typeof(SyncOutManager) }
        };

        static void checkSource (Type source, DataTag tag)
        {
            Type t;
            if (protectedTags.TryGetValue (tag, out t)) {
                if (source != t) {
                    throw new Exception (string.Format ("Type {0} cannot send {1}",
                        source.FullName, Util.GetName (tag))); 
                }
            }
        }

        public static void Init ()
        {
            StoreManager.Init ();
            SyncOutManager.Init ();
        }


        public static void Send (Type source, DataTag tag)
        {
            checkSource (source, tag);
            StoreManager.Singleton.Send (DataMsg.Success<object> (tag, null));
        }

        public static void Send<T> (Type source, DataTag tag, T data)
        {
            checkSource (source, tag);
            StoreManager.Singleton.Send (DataMsg.Success<T> (tag, data));
        }

        public static void SendError<T> (Type source, DataTag tag, Exception exc)
        {
            checkSource (source, tag);
            StoreManager.Singleton.Send (DataMsg.Error<T> (tag, exc));
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

