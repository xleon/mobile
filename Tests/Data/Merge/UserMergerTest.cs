using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class UserMergerTest : MergeTest
    {
        [Test]
        public void TestDefault()
        {
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            // Data before server push
            var merger = new UserMerger(new UserData()
            {
                Id = userId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                Name = "Initial",
                Email = "me@example.com",
                StartOfWeek = DayOfWeek.Sunday,
                DateFormat = null,
                TimeFormat = null,
                ImageUrl = null,
                Locale = "en_US",
                Timezone = "UTC",
                SendProductEmails = true,
                SendTimerNotifications = true,
                SendWeeklyReport = true,
                TrackingMode = TrackingMode.StartNew,
                DefaultWorkspaceId = workspaceId,
            });

            // Data from server
            merger.Add(new UserData()
            {
                Id = userId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                Name = "Initial",
                Email = "me@example.com",
                StartOfWeek = DayOfWeek.Sunday,
                DateFormat = null,
                TimeFormat = null,
                ImageUrl = null,
                Locale = "en_US",
                Timezone = "UTC",
                SendProductEmails = true,
                SendTimerNotifications = true,
                SendWeeklyReport = true,
                TrackingMode = TrackingMode.StartNew,
                DefaultWorkspaceId = workspaceId,
            });

            // Data changed by user in the mean time
            merger.Add(new UserData()
            {
                Id = userId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                Email = "me@example.com",
                StartOfWeek = DayOfWeek.Monday,
                DateFormat = null,
                TimeFormat = null,
                ImageUrl = null,
                Locale = "en_GB",
                Timezone = "UTC",
                SendProductEmails = false,
                SendTimerNotifications = false,
                SendWeeklyReport = false,
                TrackingMode = TrackingMode.Continue,
                DefaultWorkspaceId = workspaceId,
            });

            // Merged version
            AssertPropertiesEqual(new UserData()
            {
                Id = userId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime(2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                Email = "me@example.com",
                StartOfWeek = DayOfWeek.Monday,
                DateFormat = null,
                TimeFormat = null,
                ImageUrl = null,
                Locale = "en_GB",
                Timezone = "UTC",
                SendProductEmails = false,
                SendTimerNotifications = false,
                SendWeeklyReport = false,
                TrackingMode = TrackingMode.Continue,
                DefaultWorkspaceId = workspaceId,
            }, merger.Result);
        }
    }
}
