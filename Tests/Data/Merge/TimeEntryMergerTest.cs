using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using Toggl.Phoebe.Data;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class TimeEntryMergerTest : MergeTest
    {
        [Test]
        public void TestDefault ()
        {
            var timeEntryId = Guid.NewGuid ();
            var userId = Guid.NewGuid ();
            var projectId = Guid.NewGuid ();
            var workspaceId = Guid.NewGuid ();

            // Data before server push
            var merger = new TimeEntryMerger (new TimeEntryData () {
                Id = timeEntryId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                State = TimeEntryState.Running,
                Description = "Initial",
                StartTime = new DateTime (2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                StopTime = null,
                DurationOnly = false,
                IsBillable = true,
                UserId = userId,
                TaskId = null,
                ProjectId = null,
                WorkspaceId = workspaceId,
            });

            // Data from server
            merger.Add (new TimeEntryData () {
                Id = timeEntryId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                Description = "Initial",
                StartTime = new DateTime (2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                StopTime = null,
                DurationOnly = false,
                IsBillable = true,
                UserId = userId,
                TaskId = null,
                ProjectId = null,
                WorkspaceId = workspaceId,
            });

            // Data changed by user in the mean time
            merger.Add (new TimeEntryData () {
                Id = timeEntryId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Description = "Changed",
                StartTime = new DateTime (2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                StopTime = null,
                DurationOnly = false,
                IsBillable = true,
                UserId = userId,
                TaskId = null,
                ProjectId = projectId,
                WorkspaceId = workspaceId,
            });

            // Merged version
            AssertPropertiesEqual (new TimeEntryData () {
                Id = timeEntryId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Description = "Changed",
                StartTime = new DateTime (2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                StopTime = null,
                DurationOnly = false,
                IsBillable = true,
                UserId = userId,
                TaskId = null,
                ProjectId = projectId,
                WorkspaceId = workspaceId,
            }, merger.Result);
        }
    }
}
