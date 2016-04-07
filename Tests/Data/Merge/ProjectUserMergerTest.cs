using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class ProjectUserMergerTest : MergeTest
    {
        [Test]
        public void TestDefault()
        {
            var projectUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // Data before server push
            var merger = new ProjectUserMerger(new ProjectUserData()
            {
                Id = projectUserId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                IsManager = false,
                HourlyRate = 0,
                UserId = userId,
                ProjectId = projectId,
            });

            // Data from server
            merger.Add(new ProjectUserData()
            {
                Id = projectUserId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                IsManager = true,
                HourlyRate = 0,
                UserId = userId,
                ProjectId = projectId,
            });

            // Data changed by user in the mean time
            merger.Add(new ProjectUserData()
            {
                Id = projectUserId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                IsManager = false,
                HourlyRate = 10,
                UserId = userId,
                ProjectId = projectId,
            });

            // Merged version
            AssertPropertiesEqual(new ProjectUserData()
            {
                Id = projectUserId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                IsManager = true,
                HourlyRate = 10,
                UserId = userId,
                ProjectId = projectId,
            }, merger.Result);
        }
    }
}
