using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;

namespace Toggl.Phoebe.Tests.Data.Json.Converters
{
    public class TagJsonConverterTest : Test
    {
        private TagJsonConverter converter;

        public override void SetUp ()
        {
            base.SetUp ();

            converter = new TagJsonConverter ();
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
                var tagData = await DataStore.PutAsync (new TagData () {
                    RemoteId = 2,
                    Name = "Mobile",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var json = await converter.Export (tagData);
                Assert.AreEqual (2, json.Id);
                Assert.AreEqual ("Mobile", json.Name);
                Assert.AreEqual (new DateTime (2014, 1, 3), json.ModifiedAt);
                Assert.AreEqual (1, json.WorkspaceId);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ExportInvalidWorkspace ()
        {
            TagData tagData = null;

            RunAsync (async delegate {
                tagData = await DataStore.PutAsync (new TagData () {
                    RemoteId = 2,
                    Name = "Mobile",
                    WorkspaceId = Guid.NewGuid (),
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
            });

            Assert.That (() => converter.Export (tagData).GetAwaiter ().GetResult (),
                Throws.Exception.TypeOf<NotSupportedException> ());
        }

        [Test]
        public void ExportNew ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2),
                });
                var tagData = await DataStore.PutAsync (new TagData () {
                    Name = "Mobile",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var json = await converter.Export (tagData);
                Assert.IsNull (json.Id);
                Assert.AreEqual ("Mobile", json.Name);
                Assert.AreEqual (new DateTime (2014, 1, 3), json.ModifiedAt);
                Assert.AreEqual (1, json.WorkspaceId);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ImportNew ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2),
                });
                var tagJson = new TagJson () {
                    Id = 2,
                    Name = "Mobile",
                    WorkspaceId = 1,
                    ModifiedAt = new DateTime (2014, 1, 3),
                };

                var tagData = await converter.Import (tagJson);
                Assert.AreNotEqual (Guid.Empty, tagData.Id);
                Assert.AreEqual (2, tagData.RemoteId);
                Assert.AreEqual ("Mobile", tagData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 3), tagData.ModifiedAt);
                Assert.AreEqual (workspaceData.Id, tagData.WorkspaceId);
                Assert.IsFalse (tagData.IsDirty);
                Assert.IsFalse (tagData.RemoteRejected);
                Assert.IsNull (tagData.DeletedAt);
            });
        }

        [Test]
        public void ImportMissingWorkspace ()
        {
            RunAsync (async delegate {
                var tagJson = new TagJson () {
                    Id = 2,
                    Name = "Mobile",
                    WorkspaceId = 1,
                    ModifiedAt = new DateTime (2014, 1, 3),
                };

                var tagData = await converter.Import (tagJson);
                Assert.AreNotEqual (Guid.Empty, tagData.WorkspaceId);

                var rows = await DataStore.Table<WorkspaceData> ().QueryAsync (m => m.Id == tagData.WorkspaceId);
                var workspaceData = rows.FirstOrDefault ();
                Assert.IsNotNull (workspaceData);
                Assert.IsNotNull (workspaceData.RemoteId);
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
                var tagData = await DataStore.PutAsync (new TagData () {
                    RemoteId = 2,
                    Name = "Mobile",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var tagJson = new TagJson () {
                    Id = 2,
                    DeletedAt = new DateTime (2014, 1, 4),
                };

                var ret = await converter.Import (tagJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<TagData> ().QueryAsync (m => m.Id == tagData.Id);
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
                var tagData = await DataStore.PutAsync (new TagData () {
                    RemoteId = 2,
                    Name = "Mobile",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var tagJson = new TagJson () {
                    Id = 2,
                    DeletedAt = new DateTime (2014, 1, 2),
                };

                var ret = await converter.Import (tagJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<TagData> ().QueryAsync (m => m.Id == tagData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }
    }
}
