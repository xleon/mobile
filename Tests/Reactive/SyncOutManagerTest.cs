using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
//    [TestFixture]
    public class SyncOutManagerTest : Test
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

        public class ToggleClientMock : ITogglClient
        {
            public IList<CommonJson> ReceivedItems = new List<CommonJson> ();

            public Task<T> Create<T> (T jsonObject) where T : CommonJson
            {
                return Task.Run (() => {
                    ReceivedItems.Add (jsonObject);
                    return new object() as T;
                });
            }
            public Task<T> Get<T> (long id) where T : CommonJson
            {
                throw new NotImplementedException ();
            }
            public Task<List<T>> List<T> () where T : CommonJson
            {
                throw new NotImplementedException ();
            }
            public Task<T> Update<T> (T jsonObject) where T : CommonJson
            {
                return Task.Run (() => {
                    ReceivedItems.Add (jsonObject);
                    return new object() as T;
                });
            }
            public Task Delete<T> (T jsonObject) where T : CommonJson
            {
                return Task.Run (() => {
                    ReceivedItems.Add (jsonObject);
                    return new object() as T;
                });
            }
            public Task Delete<T> (IEnumerable<T> jsonObjects) where T : CommonJson
            {
                throw new NotImplementedException ();
            }
            public Task<UserJson> GetUser (string username, string password)
            {
                throw new NotImplementedException ();
            }
            public Task<UserJson> GetUser (string googleAccessToken)
            {
                throw new NotImplementedException ();
            }
            public Task<List<ClientJson>> ListWorkspaceClients (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<ProjectJson>> ListWorkspaceProjects (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<WorkspaceUserJson>> ListWorkspaceUsers (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TaskJson>> ListWorkspaceTasks (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TaskJson>> ListProjectTasks (long projectId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<ProjectUserJson>> ListProjectUsers (long projectId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime start, DateTime end, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime start, DateTime end)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime end, int days, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime end, int days)
            {
                throw new NotImplementedException ();
            }
            public Task<UserRelatedJson> GetChanges (DateTime? since)
            {
                throw new NotImplementedException ();
            }
            public Task CreateFeedback (FeedbackJson jsonObject)
            {
                throw new NotImplementedException ();
            }
            public Task CreateExperimentAction (ActionJson jsonObject)
            {
                throw new NotImplementedException ();
            }
        }

        ISyncDataStore dataStore;
        readonly ToggleClientMock togglClient = new ToggleClientMock ();
        readonly NetWorkPresenceMock networkPresence = new NetWorkPresenceMock ();

        public override void SetUp ()
        {
            base.SetUp ();

            var platformUtils = new PlatformUtils ();
            dataStore = new SyncSqliteDataStore (Path.GetTempFileName (), platformUtils.SQLiteInfo);

            ServiceContainer.Register<IPlatformUtils> (platformUtils);
            ServiceContainer.Register<ISyncDataStore> (dataStore);
            ServiceContainer.Register<ITogglClient> (togglClient);
            ServiceContainer.Register<Toggl.Phoebe.Net.INetworkPresence> (networkPresence);

            RxChain.Init (Util.GetInitAppState (), RxChain.InitMode.TestSyncManager);
        }

        [Test]
        public void TestSendMessageWithoutConnection ()
        {
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var oldQueueSize = dataStore.GetQueueSize (SyncOutManager.QueueId);
            networkPresence.IsNetworkPresent = false;
            togglClient.ReceivedItems.Clear ();

            subscription =
                SyncOutManager
                    .Singleton
                    .Observable
                    .Subscribe (_ => {
                        // As there's no connection, message should have been enqueued
                        Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), oldQueueSize + 1);
                        Assert.AreEqual (togglClient.ReceivedItems.Count, 0);
                        subscription.Dispose ();
                    });

            RxChain.Send (new DataMsg.TimeEntryAdd (te));
        }

        [Test]
        public void TestSendMessageWithConnection ()
        {
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var oldQueueSize = dataStore.GetQueueSize (SyncOutManager.QueueId);
            networkPresence.IsNetworkPresent = true;
            togglClient.ReceivedItems.Clear ();

            subscription =
                SyncOutManager
                    .Singleton
                    .Observable
                    .Subscribe (_ => {
                        // As there's connection, message (and pending ones) should have been sent
                        Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), 0);
                        Assert.AreEqual (togglClient.ReceivedItems.Count, oldQueueSize + 1);
                        subscription.Dispose ();
                    });

            RxChain.Send (new DataMsg.TimeEntryAdd (te));
        }

        [Test]
        public void TestTrySendMessageAndReconnect ()
        {
            int step = 0;
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var te2 = Util.CreateTimeEntryData (DateTime.Now + TimeSpan.FromMinutes(5));
            var oldQueueSize = dataStore.GetQueueSize (SyncOutManager.QueueId);
            networkPresence.IsNetworkPresent = false;
            togglClient.ReceivedItems.Clear ();

            subscription =
                SyncOutManager
                    .Singleton
                    .Observable
                    .Subscribe (_ => {
                        switch (step) {
                        case 0:
                            // As there's no connection, message should have been enqueued
                            Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), oldQueueSize + 1);
                            Assert.AreEqual (togglClient.ReceivedItems.Count, 0);
                            step++;
                            break;
                        case 1:
                            // As there's connection, message (and pending ones) should have been sent
                            Assert.AreEqual (dataStore.GetQueueSize (SyncOutManager.QueueId), 0);
                            Assert.AreEqual (togglClient.ReceivedItems.Count, oldQueueSize + 2);
                            subscription.Dispose ();
                            break;
                        }
                    });

            RxChain.Send (new DataMsg.TimeEntryAdd (te));
            networkPresence.IsNetworkPresent = true;
            RxChain.Send (new DataMsg.TimeEntryAdd (te2));
        }
    }
}

