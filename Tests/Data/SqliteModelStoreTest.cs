using System;
using System.IO;
using System.Linq.Expressions;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class SQLiteModelStoreTest : Test
    {
        private string tmpDb;

        private TestSqliteStore ModelStore {
            get { return (TestSqliteStore)ServiceContainer.Resolve<IModelStore> (); }
        }

        public override void SetUp ()
        {
            base.SetUp ();

            tmpDb = Path.GetTempFileName ();
            ServiceContainer.Register<IModelStore> (new TestSqliteStore (tmpDb));
        }

        public override void TearDown ()
        {
            ModelStore.Commit ();

            base.TearDown ();

            File.Delete (tmpDb);
            tmpDb = null;
        }

        [Test]
        public void TestPersistedModelInsertionWhenIsShared ()
        {
            var commits = 0;

            Model.Update (new PlainModel () {
                IsPersisted = true,
            });
            commits++;

            Assert.AreEqual (commits, ModelStore.ScheduledCommitCount);
            Assert.AreEqual (1, ModelStore.Query<PlainModel> ().Count ());
        }

        [Test]
        public void TestModelInsertionWhenIsPersisted ()
        {
            var commits = 0;
            var model = Model.Update (new PlainModel ());
            Assert.AreEqual (commits, ModelStore.ScheduledCommitCount);

            model.IsPersisted = true;
            commits++;

            Assert.AreEqual (commits, ModelStore.ScheduledCommitCount);
            Assert.AreEqual (1, ModelStore.Query<PlainModel> ().Count ());
        }

        [Test]
        public void TestModelInsertionWhenMerging ()
        {
            var commits = 0;

            var model = Model.Update (new PlainModel () {
                ModifiedAt = new DateTime (),
            });
            Assert.AreEqual (commits, ModelStore.ScheduledCommitCount);

            model = Model.Update (new PlainModel () {
                Id = model.Id,
                IsPersisted = true,
            });
            commits += 2; // IsPersisted, ModifiedAt has changed

            Assert.AreEqual (commits, ModelStore.ScheduledCommitCount);
            Assert.AreEqual (1, ModelStore.Query<PlainModel> ().Count ());
        }

        [Test]
        public void TestNoCommitForIgnoredFields ()
        {
            var commits = 0;

            var model = Model.Update (new PlainModel () {
                IsPersisted = true,
            });
            commits++;

            model.IsTesting = !model.IsTesting;
            Assert.AreEqual (commits, ModelStore.ScheduledCommitCount);
        }

        [Test]
        public void TestModelDeletionWhenIsPersisted ()
        {
            var commits = 0;

            var model = Model.Update (new PlainModel () {
                IsPersisted = true,
            });
            commits++;

            model.IsPersisted = false;
            commits++;

            Assert.AreEqual (commits, ModelStore.ScheduledCommitCount);
            Assert.AreEqual (0, ModelStore.Query<PlainModel> ().Count ());
        }

        private class PlainModel : Model
        {
            private static string GetPropertyName<T> (Expression<Func<PlainModel, T>> expr)
            {
                return expr.ToPropertyName ();
            }

            private bool testing;
            public static readonly string PropertyIsTesting = GetPropertyName ((m) => m.IsTesting);

            [DontDirty]
            [SQLite.Ignore]
            public bool IsTesting {
                get { return testing; }
                set {
                    if (testing == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsTesting, delegate {
                        testing = value;
                    });
                }
            }
        }

        private class TestSqliteStore : SQLiteModelStore
        {
            public int ScheduledCommitCount;

            public TestSqliteStore (string path) : base (path)
            {
            }

            protected override void CreateTables (SQLite.SQLiteConnection db)
            {
                db.CreateTable<PlainModel> ();
            }

            protected override void ScheduleCommit ()
            {
                ScheduledCommitCount++;
                Commit ();
            }
        }
    }
}
