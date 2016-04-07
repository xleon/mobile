using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class WorkspaceUserMergerTest : MergeTest
    {
        [Test]
        public void TestDefault()
        {
            var workspaceUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Data before server push
            var merger = new WorkspaceUserMerger(new WorkspaceUserData()
            {
                Id = workspaceUserId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                IsAdmin = false,
                IsActive = true,
                WorkspaceId = workspaceId,
                UserId = userId,
            });

            // Data from server
            merger.Add(new WorkspaceUserData()
            {
                Id = workspaceUserId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                IsAdmin = true,
                IsActive = true,
                WorkspaceId = workspaceId,
                UserId = userId,
            });

            // Data changed by user in the mean time
            merger.Add(new WorkspaceUserData()
            {
                Id = workspaceUserId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                IsAdmin = false,
                IsActive = false,
                WorkspaceId = workspaceId,
                UserId = userId,
            });

            // Merged version
            AssertPropertiesEqual(new WorkspaceUserData()
            {
                Id = workspaceUserId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                IsAdmin = true,
                IsActive = false,
                WorkspaceId = workspaceId,
                UserId = userId,
            }, merger.Result);
        }
    }
}
