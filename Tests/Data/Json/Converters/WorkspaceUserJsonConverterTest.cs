using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;

namespace Toggl.Phoebe.Tests.Data.Json.Converters
{
    public class WorkspaceUserJsonConverterTest : Test
    {
        private WorkspaceUserJsonConverter converter;

        public override void SetUp ()
        {
            base.SetUp ();

            converter = new WorkspaceUserJsonConverter ();
        }

        [Test]
        public void ExportExisting ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 3,
                    Name = "Hosting",
                    ModifiedAt = new DateTime (2014, 1, 2),
                });
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 5,
                    Name = "John",
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
                var workspaceUserData = await DataStore.PutAsync (new WorkspaceUserData () {
                    RemoteId = 4,
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var json = await converter.Export (workspaceUserData);
                Assert.AreEqual (4, json.Id);
                Assert.AreEqual (new DateTime (2014, 1, 3), json.ModifiedAt);
                Assert.AreEqual (3, json.WorkspaceId);
                Assert.AreEqual (5, json.UserId);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ExportInvalidWorkspaceAndUser ()
        {
            WorkspaceUserData workspaceUserData = null;

            RunAsync (async delegate {
                workspaceUserData = await DataStore.PutAsync (new WorkspaceUserData () {
                    RemoteId = 4,
                    WorkspaceId = Guid.NewGuid (),
                    UserId = Guid.NewGuid (),
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
            });

            Assert.That (() => converter.Export (workspaceUserData).GetAwaiter ().GetResult (),
                Throws.Exception.TypeOf<NotSupportedException> ());
        }

        [Test]
        public void ExportNew ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    ModifiedAt = new DateTime (2014, 1, 2),
                });
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 2,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
                var workspaceUserData = await DataStore.PutAsync (new WorkspaceUserData () {
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var json = await converter.Export (workspaceUserData);
                Assert.IsNull (json.Id);
                Assert.AreEqual (1, json.WorkspaceId);
                Assert.AreEqual (2, json.UserId);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ImportNew ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    ModifiedAt = new DateTime (2014, 1, 2),
                });
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 2,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
                var workspaceUserJson = new WorkspaceUserJson () {
                    Id = 2,
                    WorkspaceId = 1,
                    UserId = 2,
                    ModifiedAt = new DateTime (2014, 1, 3),
                };

                var workspaceUserData = await converter.Import (workspaceUserJson);
                Assert.AreNotEqual (Guid.Empty, workspaceUserData.Id);
                Assert.AreEqual (2, workspaceUserData.RemoteId);
                Assert.AreEqual (new DateTime (2014, 1, 3), workspaceUserData.ModifiedAt);
                Assert.AreEqual (workspaceData.Id, workspaceUserData.WorkspaceId);
                Assert.AreEqual (userData.Id, workspaceUserData.UserId);
                Assert.IsFalse (workspaceUserData.IsDirty);
                Assert.IsFalse (workspaceUserData.RemoteRejected);
                Assert.IsNull (workspaceUserData.DeletedAt);
            });
        }

        [Test]
        public void ImportMissingWorkspaceAndUser ()
        {
            RunAsync (async delegate {
                var workspaceUserJson = new WorkspaceUserJson () {
                    Id = 2,
                    WorkspaceId = 1,
                    UserId = 2,
                    ModifiedAt = new DateTime (2014, 1, 3),
                };

                var workspaceUserData = await converter.Import (workspaceUserJson);
                Assert.AreNotEqual (Guid.Empty, workspaceUserData.WorkspaceId);
                Assert.AreNotEqual (Guid.Empty, workspaceUserData.UserId);

                var projectRows = await DataStore.Table<WorkspaceData> ().QueryAsync (m => m.Id == workspaceUserData.WorkspaceId);
                var workspaceData = projectRows.FirstOrDefault ();
                Assert.IsNotNull (workspaceData);
                Assert.IsNotNull (workspaceData.RemoteId);

                var userRows = await DataStore.Table<UserData> ().QueryAsync (m => m.Id == workspaceUserData.UserId);
                var userData = userRows.FirstOrDefault ();
                Assert.IsNotNull (userData);
                Assert.IsNotNull (userData.RemoteId);
            });
        }

        [Test]
        public void ImportDeleted ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    ModifiedAt = new DateTime (2014, 1, 2),
                });
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 2,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
                var workspaceUserData = await DataStore.PutAsync (new WorkspaceUserData () {
                    RemoteId = 2,
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var workspaceUserJson = new WorkspaceUserJson () {
                    Id = 2,
                    DeletedAt = new DateTime (2014, 1, 4),
                };

                var ret = await converter.Import (workspaceUserJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<WorkspaceUserData> ().QueryAsync (m => m.Id == workspaceUserData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }

        [Test]
        public void ImportPastDeleted ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    ModifiedAt = new DateTime (2014, 1, 2),
                });
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 2,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
                var workspaceUserData = await DataStore.PutAsync (new WorkspaceUserData () {
                    RemoteId = 2,
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var workspaceUserJson = new WorkspaceUserJson () {
                    Id = 2,
                    DeletedAt = new DateTime (2014, 1, 2),
                };

                var ret = await converter.Import (workspaceUserJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<WorkspaceUserData> ().QueryAsync (m => m.Id == workspaceUserData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }
    }
}
