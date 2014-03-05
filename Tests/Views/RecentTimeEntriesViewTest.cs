using System;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Views
{
    [TestFixture]
    public class RecentTimeEntriesViewTest : Test
    {
        private string tmpDb;
        private WorkspaceModel workspace;
        private UserModel user;

        private IModelStore ModelStore {
            get { return ServiceContainer.Resolve<IModelStore> (); }
        }

        private AuthManager AuthManager {
            get { return ServiceContainer.Resolve<AuthManager> (); }
        }

        public override void SetUp ()
        {
            base.SetUp ();

            tmpDb = Path.GetTempFileName ();
            ServiceContainer.Register<IModelStore> (new TestSqliteStore (tmpDb));

            CreateTestData ();

            ServiceContainer.Register<ISettingsStore> (Mock.Of<ISettingsStore> (
                (store) => store.ApiToken == "test" &&
                store.UserId == user.Id));
            ServiceContainer.Register<AuthManager> (new AuthManager ());
        }

        public override void TearDown ()
        {
            ModelStore.Commit ();

            base.TearDown ();

            File.Delete (tmpDb);
            tmpDb = null;
        }

        [Test]
        public void TestInitialGrouping ()
        {
            var view = new RecentTimeEntriesView ();
            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestInitialReload ()
        {
            var view = new RecentTimeEntriesView ();
            view.Reload ();
            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestChangeGroupBottom ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (1);
            model.Description += " and some";

            Assert.AreEqual (5, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2, 1 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestChangeGroupTop ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (3);
            model.Description += " and some";

            Assert.AreEqual (5, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2, 1 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestChangeGroupBottomToTop ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (1);
            model.StopTime += TimeSpan.FromDays (1);
            model.StartTime += TimeSpan.FromDays (1);

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 1, 5, 4, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestChangeGroupTopToBottom ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (3);
            model.StopTime -= TimeSpan.FromDays (1);
            model.StartTime -= TimeSpan.FromDays (1);

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 2, 1 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestDeleteGroup ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (2);
            model.Delete ();

            Assert.AreEqual (3, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestDeleteGroupTop ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (1);
            model.Delete ();

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestDeleteGroupBottom ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (3);
            model.Delete ();

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 2, 1 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestUnPersistGroup ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (2);
            model.IsPersisted = false;

            Assert.AreEqual (3, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestUnPersistGroupTop ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (1);
            model.IsPersisted = false;

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestUnPersistGroupBottom ()
        {
            var view = new RecentTimeEntriesView ();

            var model = Model.ByRemoteId<TimeEntryModel> (3);
            model.IsPersisted = false;

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 2, 1 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestTemporaryShared ()
        {
            var view = new RecentTimeEntriesView ();

            Model.Update (new TimeEntryModel () {
                RemoteId = 6,
                State = TimeEntryState.Finished,
                StartTime = MakeTime (16, 0),
                StopTime = MakeTime (16, 36),
                Workspace = workspace,
                User = user,
            });

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestDeletedShared ()
        {
            var view = new RecentTimeEntriesView ();

            Model.Update (new TimeEntryModel () {
                RemoteId = 6,
                State = TimeEntryState.Finished,
                StartTime = MakeTime (16, 0),
                StopTime = MakeTime (16, 36),
                Workspace = workspace,
                User = user,
                IsPersisted = true,
                DeletedAt = MakeTime (16, 39),
            });

            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        [Test]
        public void TestPastEntryHidden ()
        {
            Model.Update (new TimeEntryModel () {
                RemoteId = 6,
                State = TimeEntryState.Finished,
                StartTime = MakeTime (16, 0).AddDays (-10),
                StopTime = MakeTime (16, 36).AddDays (-10),
                Workspace = workspace,
                User = user,
                IsPersisted = true,
            });

            var view = new RecentTimeEntriesView ();
            Assert.AreEqual (4, view.Count);
            Assert.AreEqual (
                new long[] { 5, 4, 3, 2 },
                view.Models.Select ((entry) => entry.RemoteId.Value).ToArray ()
            );
        }

        private void CreateTestData ()
        {
            workspace = Model.Update (new WorkspaceModel () {
                RemoteId = 1,
                Name = "Unit Testing",
                IsPersisted = true,
            });

            user = Model.Update (new UserModel () {
                RemoteId = 1,
                Name = "Tester",
                DefaultWorkspace = workspace,
                IsPersisted = true,
            });

            var project = Model.Update (new ProjectModel () {
                RemoteId = 1,
                Name = "Ad design",
                Workspace = workspace,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 1,
                Description = "Initial concept",
                State = TimeEntryState.Finished,
                StartTime = MakeTime (09, 12),
                StopTime = MakeTime (10, 1),
                Project = project,
                Workspace = workspace,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 2,
                Description = "Breakfast",
                State = TimeEntryState.Finished,
                StartTime = MakeTime (10, 5),
                StopTime = MakeTime (10, 30),
                Workspace = workspace,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 3,
                Description = "Initial concept",
                State = TimeEntryState.Finished,
                StartTime = MakeTime (10, 35),
                StopTime = MakeTime (12, 21),
                Workspace = workspace,
                Project = project,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 4,
                State = TimeEntryState.Finished,
                StartTime = MakeTime (12, 25),
                StopTime = MakeTime (13, 57),
                Workspace = workspace,
                Project = project,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 5,
                State = TimeEntryState.Finished,
                StartTime = MakeTime (14, 0),
                StopTime = MakeTime (14, 36),
                Workspace = workspace,
                User = user,
                IsPersisted = true,
            });
        }

        private DateTime MakeTime (int hour, int minute, int second = 0)
        {
            return DateTime.UtcNow.Date
                    .AddHours (hour)
                    .AddMinutes (minute)
                    .AddSeconds (second);
        }

        private class TestSqliteStore : SQLiteModelStore
        {
            public TestSqliteStore (string path) : base (path)
            {
            }

            protected override void ScheduleCommit ()
            {
                Commit ();
            }
        }
    }
}

