using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels.Timer;
using Toggl.Phoebe.Tests;
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

        readonly ToggleClientMock togglClient = new ToggleClientMock ();
        readonly NetWorkPresenceMock networkPresence = new NetWorkPresenceMock ();

        Guid userId = Guid.NewGuid ();
        Guid workspaceId = Guid.NewGuid ();
        bool messageHandled = false;
        DataTag tag = DataTag.TestSyncOutManager;

//        public override void SetUp ()
//        {
//            base.SetUp ();
//
//            ServiceContainer.Register<ITogglClient> (togglClient);
//            ServiceContainer.Register<Toggl.Phoebe.Net.INetworkPresence> (networkPresence);
//            ServiceContainer.Register<ISchedulerProvider> (new TestSchedulerProvider ());
//            ServiceContainer.Register<IPlatformUtils> (new UpgradeManagerTest.PlatformUtils ());
//
//            SyncOutManager.Init ();
//            SyncOutManager.Singleton.MessageHandled += (sender, e) => {
//                messageHandled = true;
//            };
//        }
//
//        private async Task WaitMessageHandled ()
//        {
//            while (!messageHandled) {
//                await Task.Delay (100);
//            }
//        }
//
//        // TODO: Extract these methods to a Test.Util class
//        public TimeEntryMsg CreateTimeEntryMsg (DateTime startTime, Guid taskId = default (Guid), Guid projId = default (Guid))
//        {
//            var te = new TimeEntryData {
//                Id = Guid.NewGuid (),
//                StartTime = startTime,
//                StopTime = startTime.AddMinutes (1),
//                UserId = userId,
//                WorkspaceId = workspaceId,
//                TaskId = taskId == Guid.Empty ? Guid.NewGuid () : taskId,
//                ProjectId = projId == Guid.Empty ? Guid.NewGuid () : projId,
//                Description = "Test Entry",
//                State = TimeEntryState.Finished,
//            };
//
//            return new TimeEntryMsg (DataDir.Outcoming, DataAction.Put, te);
//        }
//
//        [Test]
//        public void TestSendMessageWithoutConnection ()
//        {
//            var teMsg = CreateTimeEntryMsg (DateTime.Now);
//
//            networkPresence.IsNetworkPresent = messageHandled = false;
//
//            RunAsync (async () => {
//                var ctx = DataStore.GetSyncContext ();
//                var oldQueueSize = ctx.GetQueueSize ();
//                var oldReceivedItems = togglClient.ReceivedItems.Count;
//
//                Dispatcher.Singleton.Send (tag, teMsg);
//                await WaitMessageHandled ();
//
//                // As there's no connection, message should have been enqueued
//                Assert.AreEqual (ctx.GetQueueSize (), oldQueueSize + 1);
//                Assert.AreEqual (togglClient.ReceivedItems.Count, oldReceivedItems);
//            });
//        }
//
//        [Test]
//        public void TestSendMessageWithConnection ()
//        {
//            var teMsg = CreateTimeEntryMsg (DateTime.Now);
//            networkPresence.IsNetworkPresent = true;
//            messageHandled = false;
//
//            RunAsync (async () => {
//                var ctx = DataStore.GetSyncContext ();
//                var oldReceivedItems = togglClient.ReceivedItems.Count;
//
//                Dispatcher.Singleton.Send (tag, teMsg);
//                await WaitMessageHandled ();
//
//                // As there's connection, message should have been sent
//                Assert.AreEqual (ctx.GetQueueSize (), 0);
//                Assert.AreEqual (togglClient.ReceivedItems.Count, oldReceivedItems + 1);
//            });
//        }
//
//        [Test]
//        public void TestTrySendMessageAndReconnect ()
//        {
//            var teMsg1 = CreateTimeEntryMsg (DateTime.Now);
//            var teMsg2 = CreateTimeEntryMsg (DateTime.Now + TimeSpan.FromMinutes(5));
//            networkPresence.IsNetworkPresent = messageHandled = false;
//
//            RunAsync (async () => {
//                var ctx = DataStore.GetSyncContext ();
//                var oldQueueSize = ctx.GetQueueSize ();
//                var oldReceivedItems = togglClient.ReceivedItems.Count;
//
//                Dispatcher.Singleton.Send (tag, teMsg1);
//                await WaitMessageHandled ();
//
//                // As there's no connection, message should have been enqueued
//                Assert.AreEqual (ctx.GetQueueSize (), oldQueueSize + 1);
//                Assert.AreEqual (togglClient.ReceivedItems.Count, oldReceivedItems);
//
//                networkPresence.IsNetworkPresent = true;
//                messageHandled = false;
//
//                Dispatcher.Singleton.Send (tag, teMsg2);
//                await WaitMessageHandled ();
//
//                // As connection is back, both messages should have been sent
//                Assert.AreEqual (ctx.GetQueueSize (), 0);
//                Assert.AreEqual (togglClient.ReceivedItems.Count, oldReceivedItems + 2);
//            });
//        }
    }
}

