using System;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class RelatedDataGraphTest : Test
    {
        private WorkspaceData workspace;
        private UserData user;

        public override void SetUp ()
        {
            base.SetUp ();

            CreateTestData ();

            ServiceContainer.Register<ForeignRelationManager> ();
        }

        [Test]
        public void TestTreeRemoval ()
        {
            RunAsync (async delegate {
                var dirty = await DataStore.Table<TimeEntryData> ().QueryAsync ();
                var graph = await RelatedDataGraph.FromDirty (dirty);

                var projectRows = await DataStore.Table<ProjectData> ().QueryAsync (r => r.RemoteId == 1);
                var project = projectRows [0];
                graph.Remove (project);
            });
        }

        private void CreateTestData ()
        {
            RunAsync (async delegate {
                workspace = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "Unit Testing",
                    IsDirty = true,
                });

                user = await DataStore.PutAsync (new UserData () {
                    RemoteId = 1,
                    Name = "Tester",
                    DefaultWorkspaceId = workspace.Id,
                    IsDirty = true,
                });

                var project = await DataStore.PutAsync (new ProjectData () {
                    RemoteId = 1,
                    Name = "Ad design",
                    WorkspaceId = workspace.Id,
                    IsDirty = true,
                });

                await DataStore.PutAsync (new TimeEntryData () {
                    RemoteId = 1,
                    Description = "Initial concept",
                    State = TimeEntryState.Finished,
                    StartTime = new DateTime (2013, 01, 01, 09, 12, 0, DateTimeKind.Utc),
                    StopTime = new DateTime (2013, 01, 01, 10, 1, 0, DateTimeKind.Utc),
                    ProjectId = project.Id,
                    WorkspaceId = workspace.Id,
                    UserId = user.Id,
                    IsDirty = true,
                });

                await DataStore.PutAsync (new TimeEntryData () {
                    RemoteId = 2,
                    Description = "Breakfast",
                    State = TimeEntryState.Finished,
                    StartTime = new DateTime (2013, 01, 01, 10, 12, 0, DateTimeKind.Utc),
                    StopTime = new DateTime (2013, 01, 01, 10, 52, 0, DateTimeKind.Utc),
                    WorkspaceId = workspace.Id,
                    UserId = user.Id,
                    IsDirty = true,
                });
            });
        }
    }
}
