using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class TaskMergerTest : MergeTest
    {
        [Test]
        public void TestDefault()
        {
            var taskId = Guid.NewGuid();
            var workspace1Id = Guid.NewGuid();
            var project1Id = Guid.NewGuid();
            var workspace2Id = Guid.NewGuid();
            var project2Id = Guid.NewGuid();

            // Data before server push
            var merger = new TaskMerger(new TaskData()
            {
                Id = taskId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                Name = "Initial",
                IsActive = true,
                Estimate = 0,
                ProjectId = project1Id,
                WorkspaceId = workspace1Id,
            });

            // Data from server
            merger.Add(new TaskData()
            {
                Id = taskId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                Name = "Initial",
                IsActive = true,
                Estimate = 0,
                ProjectId = project2Id,
                WorkspaceId = workspace2Id,
            });

            // Data changed by user in the mean time
            merger.Add(new TaskData()
            {
                Id = taskId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                IsActive = true,
                Estimate = 10,
                ProjectId = project1Id,
                WorkspaceId = Guid.NewGuid(),
            });

            // Merged version
            AssertPropertiesEqual(new TaskData()
            {
                Id = taskId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                IsActive = true,
                Estimate = 10,
                ProjectId = project2Id,
                WorkspaceId = workspace2Id,
            }, merger.Result);
        }
    }
}
