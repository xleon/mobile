using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SQLite.Net;
using SQLite.Net.Interop;

namespace Toggl.Phoebe.Data
{
    interface IUpgradesTo<T>
    {
        T Upgrade(ISyncDataStoreContext context);
    }

    interface IIdentificable
    {
        Guid Id { get; }
        long? RemoteId { get; }
    }

    class MigrationException : Exception
    {
        public MigrationException(string message, Exception inner = null) : base(message, inner)
        {
        }
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
            where TOld : class, IUpgradesTo<TNew>, IIdentificable
            {
                var i = 0;
                newDB.CreateTable<TNew>();
                var list = new List<TNew>();
                foreach (var oldObj in oldDB.Table<TOld>())
                {
                    try
                    {
                        list.Add(oldObj.Upgrade(this.context));
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Cannot upgrade {typeof(TOld).Name}: Index {i} - Id {oldObj.Id} - RemoteId {oldObj.RemoteId}";
                        throw new MigrationException(msg, ex);
                    }
                    i++;
                }
                newDB.InsertAll(list);
            }
        }
    }
}