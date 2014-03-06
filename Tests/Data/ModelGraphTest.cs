using System;
using System.IO;
using Moq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class ModelGraphTest : Test
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

        [Test]
        public void TestTreeRemoval ()
        {
            var graph = ModelGraph.FromDirty (Model.Query<TimeEntryModel> ());
            graph.Remove (Model.ByRemoteId<ProjectModel> (1));
        }

        private void CreateTestData ()
        {
            workspace = Model.Update (new WorkspaceModel () {
                RemoteId = 1,
                Name = "Unit Testing",
                IsDirty = true,
                IsPersisted = true,
            });

            user = Model.Update (new UserModel () {
                RemoteId = 1,
                Name = "Tester",
                DefaultWorkspace = workspace,
                IsDirty = true,
                IsPersisted = true,
            });

            var project = Model.Update (new ProjectModel () {
                RemoteId = 1,
                Name = "Ad design",
                Workspace = workspace,
                IsDirty = true,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 1,
                Description = "Initial concept",
                State = TimeEntryState.Finished,
                StartTime = new DateTime (2013, 01, 01, 09, 12, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 01, 01, 10, 1, 0, DateTimeKind.Utc),
                Project = project,
                Workspace = workspace,
                User = user,
                IsDirty = true,
                IsPersisted = true,
            });

            Model.Update (new TimeEntryModel () {
                RemoteId = 2,
                Description = "Breakfast",
                State = TimeEntryState.Finished,
                StartTime = new DateTime (2013, 01, 01, 10, 12, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 01, 01, 10, 52, 0, DateTimeKind.Utc),
                Workspace = workspace,
                User = user,
                IsDirty = true,
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
