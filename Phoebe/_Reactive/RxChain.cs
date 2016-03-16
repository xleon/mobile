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

        public static void Init (AppState initState, InitMode mode = InitMode.Full)
        {
            switch (mode) {
            case InitMode.TestStoreManager:
                StoreManager.Init (initState, Reducers.Init ());
                break;

            // Full
            default:
                StoreManager.Init (initState, Reducers.Init ());
                SyncManager.Init ();
                break;
            }
        }

        public static void Cleanup ()
        {
            SyncManager.Cleanup ();
            StoreManager.Cleanup ();
        }

        public static void Send (DataMsg msg, SyncTestOptions syncTest = null)
        {
            StoreManager.Singleton.Send (msg, syncTest);
        }
    }
}

