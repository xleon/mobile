using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    [TestFixture]
    public class ClientMergerTest : MergeTest
    {
        [Test]
        public void TestDefault ()
        {
            var clientId = Guid.NewGuid ();
            var workspaceId = Guid.NewGuid ();

            // Data before server push
            var merger = new ClientMerger (new ClientData () {
                Id = clientId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                Name = "Initial",
                WorkspaceId = workspaceId,
            });

            // Data from server
            merger.Add (new ClientData () {
                Id = clientId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = false,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 1, DateTimeKind.Utc),
                Name = "Initial",
                WorkspaceId = workspaceId,
            });

            // Data changed by user in the mean time
            merger.Add (new ClientData () {
                Id = clientId,
                RemoteId = null,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                WorkspaceId = workspaceId,
            });

            // Merged version
            AssertPropertiesEqual (new ClientData () {
                Id = clientId,
                RemoteId = 1,
                RemoteRejected = false,
                DeletedAt = null,
                IsDirty = true,
                ModifiedAt = new DateTime (2014, 1, 10, 10, 0, 2, DateTimeKind.Utc),
                Name = "Changed",
                WorkspaceId = workspaceId,
            }, merger.Result);
        }
    }
}
