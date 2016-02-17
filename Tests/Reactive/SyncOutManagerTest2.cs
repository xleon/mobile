using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Tests;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Reactive
{
    #if __STAGING__
    [TestFixture]
    public class SyncOutManagerTest2 : Test
    {
        public class NetWorkPresenceMock : Toggl.Phoebe.Net.INetworkPresence
        {
            public bool IsNetworkPresent { get; set; }

            public void RegisterSyncWhenNetworkPresent ()
            {
                throw new NotImplementedException ();
            }

            public void UnregisterSyncWhenNetworkPresent ()
            {
                throw new NotImplementedException ();
            }
        }

        UserJson userJson;
        ISyncDataStore dataStore;
        TogglRestClient togglClient;
        readonly NetWorkPresenceMock networkPresence = new NetWorkPresenceMock ();

        public override void Init ()
        {
            base.Init ();

            dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var platformUtils = new PlatformUtils () {
                AppIdentifier = "TogglPhoebe",
                AppVersion = "0.1"
            };
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);

            togglClient = new TogglRestClient (Build.ApiUrl);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            ServiceContainer.RegisterScoped<Toggl.Phoebe.Net.INetworkPresence> (networkPresence);

            RunAsync (async () => {
                RxChain.Init (Util.GetInitAppState (), RxChain.InitMode.TestSyncManager);

                var tmpUser = new UserJson () {
                    Email = string.Format("mobile.{0}@toggl.com", Util.UserId),
                    Password = "123456",
                    Timezone = Time.TimeZoneId,
                };
                userJson = await togglClient.Create (tmpUser);
                togglClient.Authenticate (userJson.ApiToken);
            });
        }

        public override void Cleanup ()
        {
            base.Cleanup ();
            RxChain.Cleanup ();
            RunAsync (async () => {
                if (userJson != null) {
                    await togglClient.Delete (userJson);
                }
            });
        }

        [Test]
        public void TestSendMessageWithoutConnection ()
        {
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now, userJson.RemoteId.Value, userJson.DefaultWorkspaceRemoteId);
            var oldQueueSize = dataStore.GetQueueSize (SyncOutManager.QueueId);
            networkPresence.IsNetworkPresent = false;
            var tcs = Util.CreateTask<bool> ();

            subscription =
                SyncOutManager
                    .Singleton
                    .Observe ()
                    .Subscribe (remoteIds => {
                        try {
                            subscription.Dispose ();
                            // As there's no connection, message should have been enqueued
                            Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), oldQueueSize + 1);
                            Assert.AreEqual (remoteIds.Count, 0);
                            tcs.SetResult (true);
                        }
                        catch (Exception ex) {
                            tcs.SetException (ex);
                        }
                    });

            RunAsync (async () => {
                RxChain.Send (new DataMsg.TimeEntryAdd (te));
                await tcs.Task;
            });
        }

        [Test]
        public void TestSendMessageWithConnection ()
        {
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now, userJson.RemoteId.Value, userJson.DefaultWorkspaceRemoteId);
            networkPresence.IsNetworkPresent = true;
            var tcs = Util.CreateTask<bool> ();

            subscription =
                SyncOutManager
                    .Singleton
                    .Observe ()
                    .Subscribe (remoteIds => {
                        try {
                            subscription.Dispose ();
                            // As there's connection, message (and pending ones) should have been sent
                            Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), 0);
                            Assert.AreEqual (remoteIds.Count, 1);
                            tcs.SetResult (true);
                        }
                        catch (Exception ex) {
                            tcs.SetException (ex);
                        }
                    });

            RunAsync (async () => {
                RxChain.Send (new DataMsg.TimeEntryAdd (te));
                await tcs.Task;
            });
        }

//        [Test]
//        public void TestTrySendMessageAndReconnect ()
//        {
//            int step = 0;
//            IDisposable subscription = null;
//            var te = Util.CreateTimeEntryData (DateTime.Now);
//            var te2 = Util.CreateTimeEntryData (DateTime.Now + TimeSpan.FromMinutes(5));
//            var oldQueueSize = dataStore.GetQueueSize (SyncOutManager.QueueId);
//            networkPresence.IsNetworkPresent = false;
//            togglClient.ReceivedItems.Clear ();
//
//            subscription =
//                SyncOutManager
//                    .Singleton
//                    .Observable
//                    .Subscribe (_ => {
//                        switch (step) {
//                        case 0:
//                            // As there's no connection, message should have been enqueued
//                            Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), oldQueueSize + 1);
//                            Assert.AreEqual (togglClient.ReceivedItems.Count, 0);
//                            step++;
//                            break;
//                        case 1:
//                            // As there's connection, message (and pending ones) should have been sent
//                            Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), 0);
//                            Assert.AreEqual (togglClient.ReceivedItems.Count, oldQueueSize + 2);
//                            subscription.Dispose ();
//                            break;
//                        }
//                    });
//
//            RxChain.Send (new DataMsg.TimeEntryAdd (te));
//            networkPresence.IsNetworkPresent = true;
//            RxChain.Send (new DataMsg.TimeEntryAdd (te2));
//        }

    }
    #endif
}

