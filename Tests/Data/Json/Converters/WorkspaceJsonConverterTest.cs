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

                var json = await DataStore.ExecuteInTransactionAsync (ctx => converter.Export (ctx, workspaceData));
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

                var json = await DataStore.ExecuteInTransactionAsync (ctx => converter.Export (ctx, workspaceData));
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

                var workspaceData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
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
        public void ImportUpdated ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                });
                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 1, 0, DateTimeKind.Utc).ToLocalTime (), // JSON deserialized to local
                };

                workspaceData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
                Assert.AreNotEqual (Guid.Empty, workspaceData.Id);
                Assert.AreEqual (1, workspaceData.RemoteId);
                Assert.AreEqual ("Test", workspaceData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2, 10, 1, 0, DateTimeKind.Utc), workspaceData.ModifiedAt);
                Assert.IsFalse (workspaceData.IsDirty);
                Assert.IsFalse (workspaceData.RemoteRejected);
                Assert.IsNull (workspaceData.DeletedAt);
            });

            // Warn the user that the test result might be invalid
            if (TimeZone.CurrentTimeZone.GetUtcOffset (DateTime.Now).TotalMinutes >= 0) {
                Assert.Inconclusive ("The test machine timezone should be set to GTM-1 or less to test datetime comparison.");
            }
        }

        [Test]
        [Description ("Overwrite local non-dirty data regardless of the modification times.")]
        public void ImportUpdatedOverwriteNonDirtyLocal ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                });
                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2, 9, 59, 0, DateTimeKind.Utc).ToLocalTime (), // Remote modified is less than local
                };

                workspaceData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
                Assert.AreEqual ("Test", workspaceData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2, 9, 59, 0, DateTimeKind.Utc), workspaceData.ModifiedAt);
            });
        }

        [Test]
        [Description ("Overwrite dirty local data if imported data has a modification time greater than local.")]
        public void ImportUpdatedOverwriteDirtyLocal ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "",
                    ModifiedAt = new DateTime (2014, 1, 2, 9, 59, 59, DateTimeKind.Utc),
                    IsDirty = true,
                });
                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc).ToLocalTime (),
                };

                workspaceData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
                Assert.AreEqual ("Test", workspaceData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc), workspaceData.ModifiedAt);
            });
        }

        [Test]
        [Description ("Overwrite local dirty-but-rejected data regardless of the modification times.")]
        public void ImportUpdatedOverwriteRejectedLocal ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 1, 0, DateTimeKind.Utc),
                    IsDirty = true,
                    RemoteRejected = true,
                });
                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc).ToLocalTime (),
                };

                workspaceData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
                Assert.AreEqual ("Test", workspaceData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc), workspaceData.ModifiedAt);
            });
        }

        [Test]
        [Description ("Keep local dirty data when imported data has same or older modification time.")]
        public void ImportUpdatedKeepDirtyLocal ()
        {
            RunAsync (async delegate {
                var workspaceData = await DataStore.PutAsync (new WorkspaceData () {
                    RemoteId = 1,
                    Name = "",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                    IsDirty = true,
                });
                var workspaceJson = new WorkspaceJson () {
                    Id = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc).ToLocalTime (),
                };

                workspaceData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
                Assert.AreEqual ("", workspaceData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 2, 10, 0, 0, DateTimeKind.Utc), workspaceData.ModifiedAt);
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

                var ret = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
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

                var ret = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, workspaceJson));
                Assert.IsNull (ret);

                var rows = await DataStore.Table<WorkspaceData> ().QueryAsync (m => m.Id == workspaceData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }
    }
}
