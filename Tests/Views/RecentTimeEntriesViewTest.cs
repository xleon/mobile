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
                StartTime = new DateTime (2013, 10, 10, 09, 12, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 10, 10, 1, 0, DateTimeKind.Utc),
                Project = project,
                Workspace = workspace,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 2,
                Description = "Breakfast",
                StartTime = new DateTime (2013, 10, 10, 10, 5, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 10, 10, 30, 0, DateTimeKind.Utc),
                Workspace = workspace,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 3,
                Description = "Initial concept",
                StartTime = new DateTime (2013, 10, 10, 10, 35, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 10, 12, 21, 0, DateTimeKind.Utc),
                Workspace = workspace,
                Project = project,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 4,
                StartTime = new DateTime (2013, 10, 10, 12, 25, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 10, 13, 57, 0, DateTimeKind.Utc),
                Workspace = workspace,
                Project = project,
                User = user,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 5,
                StartTime = new DateTime (2013, 10, 10, 14, 0, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 10, 14, 36, 0, DateTimeKind.Utc),
                Workspace = workspace,
                User = user,
                IsPersisted = true,
            });
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

