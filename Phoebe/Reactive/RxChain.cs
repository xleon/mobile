using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Reactive
{
    public static class RxChain
    {
        public class Continuation
        {
            readonly Action<AppState, IEnumerable<ICommonData>, IEnumerable<SyncManager.QueueItem>> remoteCont;
            readonly Action<AppState> localCont;

            public bool? IsConnected { get; private set; } = null;

            public bool LocalOnly
            {
                get { return this.localCont != null; }
            }

            public Continuation(Action<AppState> localCont)
            {
                this.localCont = localCont;
            }

            public Continuation(Action<AppState, IEnumerable<ICommonData>, IEnumerable<SyncManager.QueueItem>> remoteCont = null,
                               bool? isConnected = null)
            {
                this.remoteCont = remoteCont;
                this.IsConnected = isConnected;
            }

            public void Invoke(AppState state, IEnumerable<ICommonData> remoteObjects, IEnumerable<SyncManager.QueueItem> enqueuedItems) =>
            safeInvoke(() => remoteCont(state, remoteObjects, enqueuedItems));

            public void Invoke(AppState state) => safeInvoke(() => localCont(state));

            private void safeInvoke(Action f)
            {
                try
                {
                    f();
                }
                catch (Exception ex)
                {
                    var logger = ServiceContainer.Resolve<ILogger>();
                    logger.Error(nameof(Continuation), ex, ex.Message);
                }
            }
        }

        public enum InitMode
        {
            Full,
            TestStoreManager
        }

        public static void Init(AppState initState, InitMode mode = InitMode.Full)
        {
            switch (mode)
            {
                case InitMode.TestStoreManager:
                    StoreManager.Init(initState, Reducers.Init());
                    break;

                // Full
                default:
                    StoreManager.Init(initState, Reducers.Init());
                    SyncManager.Init();
                    break;
            }
        }

        public static void Cleanup()
        {
            SyncManager.Cleanup();
            StoreManager.Cleanup();
        }

        public static void Send(ServerRequest request, Continuation cont = null) =>
        StoreManager.Singleton.Send(new DataMsg.ServerRequest(request), cont);

        public static void Send(DataMsg msg, Continuation cont = null)
        {
            if (StoreManager.Singleton != null)
            {
                StoreManager.Singleton.Send(msg, cont);
            }
            else
            {
                var logger = ServiceContainer.Resolve<ILogger>();
                logger?.Warning(nameof(RxChain), $"{msg?.GetType().Name} message received, but {nameof(StoreManager)} is not initialized");
            }
        }
    }
}

