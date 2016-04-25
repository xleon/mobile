﻿using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Reactive
{
    public static class RxChain
    {
        public class Continuation
        {
            readonly Action<AppState, IEnumerable<ICommonData>, IEnumerable<SyncManager.QueueItem>> _testCont;
            readonly Action<AppState> _cont;
            public bool LocalOnly { get; private set; }

            public Continuation(Action<AppState> cont)
            {
                _cont = cont;
                LocalOnly = true;
            }

            public Continuation(Action<AppState, IEnumerable<ICommonData>, IEnumerable<SyncManager.QueueItem>> cont)
            {
                _testCont = cont;
                LocalOnly = false;
            }

            public void Invoke(AppState state, IEnumerable<ICommonData> remoteObjects, IEnumerable<SyncManager.QueueItem> enqueuedItems) =>
            _testCont(state, remoteObjects, enqueuedItems);

            public void Invoke(AppState state) => _cont(state);
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

        public static void Send(DataMsg msg, Continuation cont = null) =>
        StoreManager.Singleton.Send(msg, cont);
    }
}
