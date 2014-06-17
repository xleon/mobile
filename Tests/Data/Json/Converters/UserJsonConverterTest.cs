using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;

namespace Toggl.Phoebe.Tests.Data.Json.Converters
{
    public class UserJsonConverterTest : Test
    {
        private UserJsonConverter converter;

        public override void SetUp ()
        {
            base.SetUp ();

            converter = new UserJsonConverter ();
        }

        [Test]
        public void ExportExisting ()
        {
            RunAsync (async delegate {
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 1,
                    Name = "John",
                    ModifiedAt = new DateTime (2014, 1, 4),
                });

                var json = await converter.Export (userData);
                Assert.AreEqual (1, json.Id);
                Assert.AreEqual ("John", json.Name);
                Assert.AreEqual (new DateTime (2014, 1, 4), json.ModifiedAt);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ExportInvalidWorkspace ()
        {
            UserData userData = null;

            RunAsync (async delegate {
                userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 1,
                    Name = "John",
                    DefaultWorkspaceId = Guid.NewGuid (),
                    ModifiedAt = new DateTime (2014, 1, 3),
                });
            });

            Assert.That (() => converter.Export (userData).GetAwaiter ().GetResult (),
                Throws.Exception.TypeOf<NotSupportedException> ());
        }

        [Test]
        public void ExportNew ()
        {
            RunAsync (async delegate {
                var userData = await DataStore.PutAsync (new UserData () {
                    Name = "John",
                    ModifiedAt = new DateTime (2014, 1, 4),
                });

                var json = await converter.Export (userData);
                Assert.IsNull (json.Id);
                Assert.AreEqual ("John", json.Name);
                Assert.AreEqual (new DateTime (2014, 1, 4), json.ModifiedAt);
                Assert.IsNull (json.DeletedAt);
            });
        }

        [Test]
        public void ImportNew ()
        {
            RunAsync (async delegate {
                var userJson = new UserJson () {
                    Id = 1,
                    Name = "John",
                    ModifiedAt = new DateTime (2014, 1, 4),
                };

                var userData = await converter.Import (userJson);
                Assert.AreNotEqual (Guid.Empty, userData.Id);
                Assert.AreEqual (1, userData.RemoteId);
                Assert.AreEqual ("John", userData.Name);
                Assert.AreEqual (new DateTime (2014, 1, 4), userData.ModifiedAt);
                Assert.IsFalse (userData.IsDirty);
                Assert.IsFalse (userData.RemoteRejected);
                Assert.IsNull (userData.DeletedAt);
            });
        }

        [Test]
        public void ImportDeleted ()
        {
            RunAsync (async delegate {
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 1,
                    Name = "John",
                    ModifiedAt = new DateTime (2014, 1, 4),
                });

                var userJson = new UserJson () {
                    Id = 1,
                    DeletedAt = new DateTime (2014, 1, 6),
                };

                var ret = await converter.Import (userJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<UserData> ().QueryAsync (m => m.Id == userData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }

        [Test]
        public void ImportPastDeleted ()
        {
            RunAsync (async delegate {
                var userData = await DataStore.PutAsync (new UserData () {
                    RemoteId = 1,
                    Name = "John",
                    ModifiedAt = new DateTime (2014, 1, 4),
                });

                var userJson = new UserJson () {
                    Id = 1,
                    DeletedAt = new DateTime (2014, 1, 2),
                };

                var ret = await converter.Import (userJson);
                Assert.IsNull (ret);

                var rows = await DataStore.Table<UserData> ().QueryAsync (m => m.Id == userData.Id);
                Assert.That (rows, Has.Exactly (0).Count);
            });
        }
    }
}
