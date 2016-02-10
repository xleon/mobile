using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Helpers;

namespace Toggl.Phoebe._Reactive
{
    public static class RxChain
    {
        public enum InitMode {
            Full,
            TestStoreManager
        }

        // TODO: Put protected tags here
        readonly static Dictionary<DataTag, Type> protectedTags = new Dictionary<DataTag, Type> ();

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

        public static void Init (AppState initState, InitMode mode = InitMode.Full)
        {
            switch (mode) {
            case InitMode.TestStoreManager:
                StoreManager.Init (initState, Reducers.Init ());
                break;

            // Full
            default:
                StoreManager.Init (initState, Reducers.Init ());
                SyncOutManager.Init ();
                break;
            }
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
    }
}

