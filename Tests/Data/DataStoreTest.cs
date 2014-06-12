using System;
using System.IO;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class DataStoreTest : Test
    {
        private string tmpDb;

        private IDataStore DataStore {
            get { return ServiceContainer.Resolve<IDataStore> (); }
        }

        public override void SetUp ()
        {
            base.SetUp ();

            tmpDb = Path.GetTempFileName ();
            ServiceContainer.Register<IDataStore> (new SQLiteDataStore (tmpDb));
        }

        public override void TearDown ()
        {
            base.TearDown ();

            File.Delete (tmpDb);
            tmpDb = null;
        }

        [Test]
        public void TestObjectCreation ()
        {
            RunAsync (async delegate {
                var obj1 = new WorkspaceData () {
                    Name = "Test",
                };

                var obj2 = await DataStore.PutAsync (obj1);

                Assert.AreNotSame (obj1, obj2, "Put should return a new instance of the object.");
                Assert.AreNotEqual (obj1.Id, obj2.Id, "Primary key was not set!");
            });
        }

        [Test]
        public void TestObjectUpdating ()
        {
            RunAsync (async delegate {
                // Create new object
                var obj1 = await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test",
                });

                // Update it
                obj1.Name = "Other";
                var obj2 = await DataStore.PutAsync (obj1);

                Assert.AreNotSame (obj1, obj2, "Put should return a new instance of the object.");
                Assert.AreEqual ("Other", obj2.Name, "Property was not updated.");
            });
        }

        [Test]
        public void TestObjectDeleting ()
        {
            RunAsync (async delegate {
                // Create new object
                var obj1 = await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test",
                });

                // Delete it
                var success = await DataStore.DeleteAsync (obj1);
                Assert.IsTrue (success, "Object was not deleted.");
            });
        }

        [Test]
        public void TestFailedDelete ()
        {
            RunAsync (async delegate {
                // Create non-existing object
                var obj1 = new WorkspaceData () {
                    Id = Guid.NewGuid (),
                    Name = "Test",
                };

                // Delete it
                var success = await DataStore.DeleteAsync (obj1);
                Assert.IsFalse (success, "Delete should've failed.");
            });
        }

        [Test]
        public void TestQuery ()
        {
            RunAsync (async delegate {
                // Create some data
                await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test #1",
                });
                await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test #2",
                });
                await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Foo #1",
                });

                var count = await DataStore.Table<WorkspaceData> ().CountAsync ();
                Assert.AreEqual (3, count, "Query returned false count for items.");

                count = await DataStore.Table<WorkspaceData> ().Where ((m) => m.Name.StartsWith ("Foo")).CountAsync ();
                Assert.AreEqual (1, count, "Query returned false count for items starting with Foo");

                var data = await DataStore.Table<WorkspaceData> ().QueryAsync ((m) => m.Name.StartsWith ("Test"));
                foreach (var obj in data) {
                    Assert.AreNotEqual ("Foo #1", obj.Name);
                }

                data = await DataStore.Table<WorkspaceData> ()
                    .OrderBy ((m) => m.Name)
                    .Take (1).Skip (1)
                    .QueryAsync ((m) => m.Name.StartsWith ("Test"));
                Assert.AreEqual (1, data.Count, "Should've received only a single result");
                Assert.AreEqual ("Test #2", data [0].Name, "Invalid item returned");
            });
        }

        [Test]
        public void TestTransaction ()
        {
            RunAsync (async delegate {
                await DataStore.ExecuteInTransactionAsync ((ctx) => {
                    var obj1 = ctx.Put (new WorkspaceData () {
                        Name = "Test",
                    });

                    var obj2 = ctx.Connection.Get<WorkspaceData> (obj1.Id);
                    Assert.IsNotNull (obj2);
                    Assert.AreEqual (obj1.Id, obj2.Id);
                    Assert.AreEqual (obj1.Name, obj2.Name);
                });
            });
        }
    }
}
