using System;
using NUnit.Framework;
using Toggl.Phoebe.Net;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class SyncOutManagerTest : Test
    {
        public class NetWorkPresenceMock : INetworkPresence
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

        UserData user;
        WorkspaceData workspace;
        NetWorkPresenceMock networkPresence = new NetWorkPresenceMock ();

        public override void SetUp ()
        {
            base.SetUp ();

            RunAsync (async () => {
                ServiceContainer.Register<INetworkPresence> (networkPresence);
                ServiceContainer.Register<ISchedulerProvider> (new TestSchedulerProvider ());
                ServiceContainer.Register<IPlatformUtils> (new UpgradeManagerTest.PlatformUtils ());

                workspace = await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test",
                    RemoteId = 9999
                });
                user = await DataStore.PutAsync (new UserData () {
                    Name = "John Doe",
                    TrackingMode = TrackingMode.StartNew,
                    DefaultWorkspaceId = workspace.Id,
                    StartOfWeek = DayOfWeek.Monday,
                });
                await SetUpFakeUser (user.Id);

                SyncOutManager.Init ();
            });
        }

        [Test]
        public void TestSendMessageNoConnection ()
        {
            var teData = new TimeEntryData {
                RemoteId = 123,
                Description = "description",
                IsBillable = true,
                ProjectRemoteId = 123,
                DurationOnly = true,
                StartTime = DateTime.Now,
                TaskRemoteId = null,
                UserRemoteId = 333,
                WorkspaceRemoteId = 222,
                State = TimeEntryState.Running
            };

            var teMsg = new TimeEntryMsg (DataDir.Outcoming, DataAction.Put, teData);

            networkPresence.IsNetworkPresent = false;

            RunAsync (async () =>
                await DataStore.ExecuteInTransactionSilent (ctx => {
                    var oldQueueSize = ctx.GetQueueSize ();
                    Dispatcher.Singleton.Send (DataTag.TestSyncOutManager, teMsg);

                    // As there's no connection, message should have been enqueued
                    Assert.AreEqual (ctx.GetQueueSize (), oldQueueSize + 1);
                })
             );
        }
    }
}

