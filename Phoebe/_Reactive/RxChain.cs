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
            TestStoreManager,
            TestSyncManager
        }

        public static void Init (AppState initState, InitMode mode = InitMode.Full)
        {
            switch (mode) {
            case InitMode.TestStoreManager:
                StoreManager.Init (initState, Reducers.Init (), new TestSchedulerProvider ());
                break;

            case InitMode.TestSyncManager:
                StoreManager.Init (initState, Reducers.Init (), new TestSchedulerProvider ());
                SyncOutManager.Init ();
                break;

            // Full
            default:
                StoreManager.Init (initState, Reducers.Init (), new DefaultSchedulerProvider ());
                SyncOutManager.Init ();
                break;
            }
        }

        public static void Send (DataMsg msg)
        {
            StoreManager.Singleton.Send (msg);
        }
    }
}

