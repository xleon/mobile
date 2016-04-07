using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class ProjectMergerTest : MergeTest
    {
        [Test]
        public void TestDefault()
        {
            var projectId = Guid.NewGuid();
            var client1Id = Guid.NewGuid();
            var workspace1Id = Guid.NewGuid();
            var client2Id = Guid.NewGuid();
            var workspace2Id = Guid.NewGuid();

            // Data before server push
            var merger = new ProjectMerger(new ProjectData()
            {
                Id = projectId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                Name = "Initial",
                Color = 100000,
                IsActive = true,
                IsBillable = false,
                IsPrivate = false,
                IsTemplate = false,
                UseTasksEstimate = false,
                ClientId = client1Id,
                WorkspaceId = workspace1Id,
            });

            // Data from server
            merger.Add(new ProjectData()
            {
                Id = projectId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                Name = "Initial",
                Color = 0,
                IsActive = true,
                IsBillable = false,
                IsPrivate = false,
                IsTemplate = false,
                UseTasksEstimate = false,
                ClientId = client2Id,
                WorkspaceId = workspace2Id,
            });

            // Data changed by user in the mean time
            merger.Add(new ProjectData()
            {
                Id = projectId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                Color = 1,
                IsActive = true,
                IsBillable = false,
                IsPrivate = false,
                IsTemplate = false,
                UseTasksEstimate = false,
                ClientId = client1Id,
                WorkspaceId = Guid.NewGuid(),
            });

            // Merged version
            AssertPropertiesEqual(new ProjectData()
            {
                Id = projectId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                Color = 1,
                IsActive = true,
                IsBillable = false,
                IsPrivate = false,
                IsTemplate = false,
                UseTasksEstimate = false,
                ClientId = client2Id,
                WorkspaceId = workspace2Id,
            }, merger.Result);
        }
    }
}
