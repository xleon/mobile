using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;

namespace Toggl.Phoebe.Tests.Data.Json.Converters
{
    public class WorkspaceJsonConverterTest : Test
    {
        private WorkspaceJsonConverter converter;

        public override void SetUp ()
        {
            base.SetUp ();

            converter = new WorkspaceJsonConverter ();
        }

        [Test]
        public void ExportExisting ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2),
                });

                var json = await converter.Export (workspaceData);
                Assert.AreEqual (1, json.Id);
                Assert.AreEqual ("Test", json.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2), json.ModifiedAt);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ExportNew ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2),
                });

                var json = await converter.Export (workspaceData);
                Assert.IsNull (json.Id);
                Assert.AreEqual ("Test", json.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2), json.ModifiedAt);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ImportNew ()
        {
            RunAsync (async delegate {
                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2),
                };

                var workspaceData = await converter.Import (workspaceJson);
                Assert.AreNotEqual (Guid.Empty, workspaceData.Id);
                Assert.AreEqual (1, workspaceData.RemoteId);
                Assert.AreEqual ("Test", workspaceData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2), workspaceData.ModifiedAt);
                Assert.IsFalse (workspaceData.IsDirty);
                Assert.IsFalse (workspaceData.RemoteRejected);
                Assert.IsNull (workspaceData.DeletedAt);
            });
        }

        [Test]
        public void ImportDeleted ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2),
                });

                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    DeletedAt = new DateTime (2014, 1, 4),
                };

                var ret = await converter.Import (workspaceJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<WorkspaceData> ().QueryAsync (m => m.Id == workspaceData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }

        [Test]
        public void ImportPastDeleted ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2),
                });

                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    DeletedAt = new DateTime (2014, 1, 1),
                };

                var ret = await converter.Import (workspaceJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<WorkspaceData> ().QueryAsync (m => m.Id == workspaceData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }
    }
}
