using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Net;

namespace Toggl.Phoebe.Data
{
    interface IUpgradesTo<T>
    {
        T Upgrade(ISyncDataStoreContext context);
    }

    abstract class DatabaseMigrator
    {
        #region known migrators

        private static readonly Dictionary<int, DatabaseMigrator> migrators =
            new DatabaseMigrator[]
        {
            new Models.Old.DB_VERSION_0.Migrator()
        } .ToDictionary(m => m.OldVersion);

        #endregion

        public static DatabaseMigrator ForVersion(int version)
        {
            DatabaseMigrator migrator;
            migrators.TryGetValue(version, out migrator);
            return migrator;
        }

        #region implementation

        public int OldVersion { get; }
        public int NewVersion { get; }

        protected DatabaseMigrator(int oldVersion, int newVersion)
        {
            this.OldVersion = oldVersion;
            this.NewVersion = newVersion;
        }

        protected abstract IEnumerable<Action<UpgradeContext>> upgraders { get; }

        public void Migrate(SQLiteConnection oldDB, SQLiteConnection newDB)
        {
            var upgradeContext = new UpgradeContext(oldDB, newDB);

            configureDatabaseForVersion(newDB, this.NewVersion);

            foreach (var upgrader in this.upgraders)
            {
                upgrader(upgradeContext);
            }
        }

        void configureDatabaseForVersion(SQLiteConnection db, int version)
        {
            db.CreateTable<SyncSqliteDataStore.MetaData>();
            db.InsertOrIgnore(SyncSqliteDataStore.MetaData.Create(nameof(SyncSqliteDataStore.DB_VERSION), version));
        }

        #endregion

        public class UpgradeContext
        {
            private readonly SQLiteConnection oldDB;
            private readonly SQLiteConnection newDB;
            private readonly SyncSqliteDataStore.SyncSqliteDataStoreContext context;

            public UpgradeContext(SQLiteConnection oldDB, SQLiteConnection newDB)
            {
                this.oldDB = oldDB;
                this.newDB = newDB;

                this.context =  new SyncSqliteDataStore.SyncSqliteDataStoreContext(oldDB);
            }

            public void Upgrade<TOld, TNew>()
            where TOld : class, IUpgradesTo<TNew>
            {
                newDB.CreateTable<TNew>();
                var list = new List<TNew>();
                foreach (var oldObj in oldDB.Table<TOld>())
                {
                    list.Add(oldObj.Upgrade(this.context));
                }
                newDB.InsertAll(list);
            }
        }
    }
}