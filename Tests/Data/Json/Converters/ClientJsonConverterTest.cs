using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;

namespace Toggl.Phoebe.Tests.Data.Json.Converters
{
    public class ClientJsonConverterTest : Test
    {
        private ClientJsonConverter converter;

        public override void SetUp ()
        {
            base.SetUp ();

            converter = new ClientJsonConverter ();
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
                var clientData = await DataStore.PutAsync (new ClientData () {
                    RemoteId = 2,
                    Name = "Github",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var json = await DataStore.ExecuteInTransactionAsync (ctx => converter.Export (ctx, clientData));
                Assert.AreEqual (2, json.Id);
                Assert.AreEqual ("Github", json.Name);
                Assert.AreEqual (new DateTime (2014, 1, 3), json.ModifiedAt);
                Assert.AreEqual (1, json.WorkspaceId);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ExportInvalidWorkspace ()
        {
            ClientData clientData = null;

            RunAsync (async delegate {
                clientData = await DataStore.PutAsync (new ClientData () {
                    RemoteId = 2,
                    Name = "Github",
                    WorkspaceId = Guid.NewGuid (),
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
            });

            Assert.That (() => RunAsync (async delegate {
                await DataStore.ExecuteInTransactionAsync (ctx => converter.Export (ctx, clientData));
            }), Throws.Exception.TypeOf<InvalidOperationException> ());
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
                var clientData = await DataStore.PutAsync (new ClientData () {
                    Name = "Github",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var json = await DataStore.ExecuteInTransactionAsync (ctx => converter.Export (ctx, clientData));
                Assert.IsNull (json.Id);
                Assert.AreEqual ("Github", json.Name);
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
                var clientJson = new ClientJson () {
                    Id = 2,
                    Name = "Github",
                    WorkspaceId = 1,
                    ModifiedAt = new DateTime (2014, 1, 3),
                };

                var clientData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, clientJson));
                Assert.AreNotEqual (Guid.Empty, clientData.Id);
                Assert.AreEqual (2, clientData.RemoteId);
                Assert.AreEqual ("Github", clientData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 3), clientData.ModifiedAt);
                Assert.AreEqual (workspaceData.Id, clientData.WorkspaceId);
                Assert.IsFalse (clientData.IsDirty);
                Assert.IsFalse (clientData.RemoteRejected);
                Assert.IsNull (clientData.DeletedAt);
            });
        }

        [Test]
        public void ImportMissingWorkspace ()
        {
            RunAsync (async delegate {
                var clientJson = new ClientJson () {
                    Id = 2,
                    Name = "Github",
                    WorkspaceId = 1,
                    ModifiedAt = new DateTime (2014, 1, 3),
                };

                var clientData = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, clientJson));
                Assert.AreNotEqual (Guid.Empty, clientData.WorkspaceId);

                var rows = await DataStore.Table<WorkspaceData> ().QueryAsync (m => m.Id == clientData.WorkspaceId);
                var workspaceData = rows.FirstOrDefault ();
                Assert.IsNotNull (workspaceData);
                Assert.IsNotNull (workspaceData.RemoteId);
                Assert.AreEqual (DateTime.MinValue, workspaceData.ModifiedAt);
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
                var clientData = await DataStore.PutAsync (new ClientData () {
                    RemoteId = 2,
                    Name = "Github",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var clientJson = new ClientJson () {
                    Id = 2,
                    DeletedAt = new DateTime (2014, 1, 4),
                };

                var ret = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, clientJson));
                Assert.IsNull (ret);

                var rows = await DataStore.Table<ClientData> ().QueryAsync (m => m.Id == clientData.Id);
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
                var clientData = await DataStore.PutAsync (new ClientData () {
                    RemoteId = 2,
                    Name = "Github",
                    WorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime (2014, 1, 3),
                });

                var clientJson = new ClientJson () {
                    Id = 2,
                    DeletedAt = new DateTime (2014, 1, 2),
                };

                var ret = await DataStore.ExecuteInTransactionAsync (ctx => converter.Import (ctx, clientJson));
                Assert.IsNull (ret);

                var rows = await DataStore.Table<ClientData> ().QueryAsync (m => m.Id == clientData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }
    }
}
