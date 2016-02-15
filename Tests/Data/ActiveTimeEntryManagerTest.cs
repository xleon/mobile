using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Tests.Analytics;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class ActiveTimeEntryManagerTest : Test
    {
        private UserData user;
        private WorkspaceData workspace;

        public override void SetUp ()
        {
            base.SetUp ();

            RunAsync (async delegate {
                workspace = await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test",
                });

                user = await DataStore.PutAsync (new UserData () {
                    Name = "John Doe",
                    TrackingMode = TrackingMode.StartNew,
                    DefaultWorkspaceId = workspace.Id,
                });

                await SetUpFakeUser (user.Id);
                var activeManager = new ActiveTimeEntryManager ();
                await Util.AwaitPredicate (() => activeManager.ActiveTimeEntry != null);

                ServiceContainer.Register<ExperimentManager> (new ExperimentManager ());
                ServiceContainer.Register<ISyncManager> (Mock.Of<ISyncManager> (mgr => !mgr.IsRunning));
                ServiceContainer.Register<ActiveTimeEntryManager> (activeManager);
                ServiceContainer.Register<ITracker> (() => new FakeTracker ());
            });
        }

        private ActiveTimeEntryManager ActiveManager
        {
            get { return ServiceContainer.Resolve<ActiveTimeEntryManager> (); }
        }

        private void StopSync ()
        {
            MessageBus.Send (new SyncFinishedMessage (
                                 ServiceContainer.Resolve<ISyncManager> (),
                                 SyncMode.Full, false, null));
        }

        private async Task<TimeEntryData> GetEntry (Guid id)
        {
            return (await DataStore.Table<TimeEntryData> ().Where (r => r.Id == id).ToListAsync ()).Single ();
        }

        [Test]
        public void TestActiveEntryAfterSync ()
        {
            RunAsync (async delegate {
                var startTime = Time.UtcNow - TimeSpan.FromHours (1);
                var te1 = await DataStore.PutAsync (new TimeEntryData () {
                    Description = "Morning meeting",
                    State = TimeEntryState.Running,
                    StartTime = startTime,
                    ModifiedAt = startTime,
                    IsDirty = true,
                    WorkspaceId = workspace.Id,
                    UserId = user.Id,
                });

                Assert.AreNotEqual (ActiveManager.ActiveTimeEntry, te1.Id);

                ActiveManager.PropertyChanged += async (sender, e) => {
                    te1 = await GetEntry (te1.Id);
                    Assert.AreEqual (ActiveManager.ActiveTimeEntry.Id, te1.Id);
                    Assert.AreEqual (ActiveManager.ActiveTimeEntry.State, te1.State);
                };

                // Send sync finished message.
                StopSync ();
            });
        }

        [Test]
        public void TestActiveEntryAfterStartStop ()
        {
            RunAsync (async delegate {

                var te1 = TimeEntryModel.GetDraft ();
                Assert.AreEqual (te1.State, TimeEntryState.New);

                te1 = await TimeEntryModel.StartAsync (te1);

                Assert.AreEqual (te1.Id, ActiveManager.ActiveTimeEntry.Id);
                Assert.IsTrue (ActiveManager.IsRunning);
                Assert.AreEqual (te1.State, TimeEntryState.Running);

                te1 = await TimeEntryModel.StopAsync (te1);

                Assert.AreNotEqual (te1.Id, ActiveManager.ActiveTimeEntry.Id);
                Assert.IsFalse (ActiveManager.IsRunning);
                Assert.AreEqual (te1.State, TimeEntryState.Finished);
            });
        }

        [Test]
        public void TestActiveEntryAfterContinue()
        {
            RunAsync (async delegate {

                var startTime = Time.UtcNow - TimeSpan.FromHours (2);
                var te1 = await DataStore.PutAsync (new TimeEntryData {
                    Description = "Morning meeting",
                    State = TimeEntryState.Finished,
                    StartTime = startTime,
                    ModifiedAt = startTime,
                    IsDirty = true,
                    WorkspaceId = workspace.Id,
                    UserId = user.Id,
                });

                startTime = Time.UtcNow - TimeSpan.FromHours (1);
                var te2 = await DataStore.PutAsync (new TimeEntryData {
                    Description = "Morning coffee",
                    State = TimeEntryState.Finished,
                    StartTime = startTime,
                    ModifiedAt = startTime,
                    IsDirty = true,
                    WorkspaceId = workspace.Id,
                    UserId = user.Id,
                });

                Assert.AreNotEqual (te1.Id, ActiveManager.ActiveTimeEntry.Id);
                Assert.AreNotEqual (te2.Id, ActiveManager.ActiveTimeEntry.Id);

                te1 = await TimeEntryModel.ContinueAsync (te1);
                te2 = await GetEntry (te2.Id);

                Assert.AreEqual (te1.Id, ActiveManager.ActiveTimeEntry.Id);
                Assert.IsTrue (ActiveManager.IsRunning);
                Assert.AreEqual (te1.State, TimeEntryState.Running);
                Assert.AreEqual (te2.State, TimeEntryState.Finished);

                te2 = await TimeEntryModel.ContinueAsync (te2);
                te1 = await GetEntry (te1.Id);

                Assert.AreEqual (te2.Id, ActiveManager.ActiveTimeEntry.Id);
                Assert.IsTrue (ActiveManager.IsRunning);
                Assert.AreEqual (te2.State, TimeEntryState.Running);
                Assert.AreEqual (te1.State, TimeEntryState.Finished);
            });
        }

        [Test]
        public void TestUserLogin ()
        {
            RunAsync (async delegate {
                // Fake user change:
                var newUser = await DataStore.PutAsync (new UserData () {
                    Name = "Jane Doe",
                    TrackingMode = TrackingMode.StartNew,
                    DefaultWorkspaceId = workspace.Id,
                });

                // Wait for ActiveTimeEntryManager initialization.
                ActiveManager.PropertyChanged += async (sender, e) => {
                    if (e.PropertyName == ActiveTimeEntryManager.PropertyActiveTimeEntry) {

                        Assert.AreNotEqual (newUser.Id, ActiveManager.ActiveTimeEntry.UserId);
                        await SetUpFakeUser (newUser.Id);
                        Assert.AreEqual (newUser.Id, ActiveManager.ActiveTimeEntry.UserId);
                    }
                };
            });
        }

        [Test]
        public void TestUserLogout ()
        {
            RunAsync (async delegate {
                var te1 = TimeEntryModel.GetDraft ();
                te1 = await TimeEntryModel.StartAsync (te1);

                Assert.AreEqual (te1.Id, ActiveManager.ActiveTimeEntry.Id);
                Assert.AreEqual (user.Id, ActiveManager.ActiveTimeEntry.UserId);

                ActiveManager.PropertyChanged += (sender, e) => {
                    Assert.AreEqual (ActiveManager.ActiveTimeEntry.UserId, Guid.Empty);
                    Assert.AreEqual (ActiveManager.ActiveTimeEntry.State, TimeEntryState.New);
                };
                ServiceContainer.Resolve <AuthManager> ().Forget ();
            });
        }

        [Test]
        public void TestInitialDraft ()
        {
            ActiveManager.PropertyChanged += (sender, e) => Assert.AreEqual (user.Id, ActiveManager.ActiveTimeEntry.UserId);
        }
    }
}
