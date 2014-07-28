using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class WorkspaceMergerTest : MergeTest
    {
        [Test]
        public void TestDefault ()
        {
            var workspaceId = Guid.NewGuid ();

            // Data before server push
            var merger = new WorkspaceMerger (new WorkspaceData () {
                Id = workspaceId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                Name = "Initial",
                IsPremium = false,
                DefaultRate = 10,
                DefaultCurrency = "EUR",
                ProjectCreationPrivileges = AccessLevel.Admin,
                BillableRatesVisibility = AccessLevel.Admin,
                RoundingMode = RoundingMode.Nearest,
                RoundingPercision = 2,
                LogoUrl = null,
            });

            // Data from server
            merger.Add (new WorkspaceData () {
                Id = workspaceId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                Name = "Initial",
                IsPremium = true,
                DefaultRate = 10,
                DefaultCurrency = "EUR",
                ProjectCreationPrivileges = AccessLevel.Admin,
                BillableRatesVisibility = AccessLevel.Admin,
                RoundingMode = RoundingMode.Nearest,
                RoundingPercision = 2,
                LogoUrl = "http://...",
            });

            // Data changed by user in the mean time
            merger.Add (new WorkspaceData () {
                Id = workspaceId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                IsPremium = false,
                DefaultRate = 10,
                DefaultCurrency = "EUR",
                ProjectCreationPrivileges = AccessLevel.Admin,
                BillableRatesVisibility = AccessLevel.Admin,
                RoundingMode = RoundingMode.Nearest,
                RoundingPercision = 2,
                LogoUrl = null,
            });

            // Merged version
            AssertPropertiesEqual (new WorkspaceData () {
                Id = workspaceId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                IsPremium = true,
                DefaultRate = 10,
                DefaultCurrency = "EUR",
                ProjectCreationPrivileges = AccessLevel.Admin,
                BillableRatesVisibility = AccessLevel.Admin,
                RoundingMode = RoundingMode.Nearest,
                RoundingPercision = 2,
                LogoUrl = "http://...",
            }, merger.Result);
        }
    }
}
