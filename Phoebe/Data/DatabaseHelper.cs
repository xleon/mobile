using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SQLite.Net;
using SQLite.Net.Interop;

namespace Toggl.Phoebe.Data
{
    static class DatabaseHelper
    {
        public static int GetVersion(SQLiteConnection connection)
        {
            var tableInfo = connection.GetTableInfo(nameof(SyncSqliteDataStore.MetaData));
            if (tableInfo == null || tableInfo.Count == 0)
                return 0;
            var data = connection.Table<SyncSqliteDataStore.MetaData>()
                       .Where(x => x.Id == nameof(SyncSqliteDataStore.DB_VERSION))
                       .FirstOrDefault();
            return data?.Convert<int>() ?? 0;
        }

        public static void Migrate(SQLiteConnection connection, ISQLitePlatform platformInfo,
                                   string dbPath, int desiredVersion)
        {
            var migrateFromDB = connection;
            var version = GetVersion(migrateFromDB);

            var tempDBPath = dbPath + ".migrated";

            while (true)
            {
                var migrator = DatabaseMigrator.ForVersion(version);

                var expectedNewVersion = migrator.NewVersion;

                validateMigrator(version, migrator, desiredVersion);

                var newDB = new SQLiteConnection(platformInfo, expectedNewVersion == desiredVersion
                                                 ? tempDBPath
                                                 : "Data Source =: memory:");

                migrator.Migrate(migrateFromDB, newDB);
                migrateFromDB.Close();

                var newVersion = GetVersion(newDB);

                validateMigratedVersion(version, expectedNewVersion, newVersion, desiredVersion);

                if (newVersion == desiredVersion)
                {
                    newDB.Close();
                    replaceDatabase(dbPath, tempDBPath);
                    return;
                }

                migrateFromDB = newDB;
            }
        }

        private static void replaceDatabase(string dbPath, string tempDBPath)
        {
            var dbDeletionPath = dbPath + ".old";
            File.Move(dbPath, dbDeletionPath);
            try
            {
                File.Move(tempDBPath, dbPath);
            }
            catch
            {
                // in case move fails, attempt to restore original data base so user can try again
                File.Move(dbDeletionPath, dbPath);
                throw;
            }
            File.Delete(dbDeletionPath);
        }

        private static void validateMigratedVersion(int oldVersion, int expectedNewVersion, int newVersion, int desiredVersion)
        {
            if (newVersion != expectedNewVersion)
                throw new Exception($"Expected new database version {expectedNewVersion}, but was {newVersion}");

            if (newVersion <= oldVersion || newVersion > desiredVersion)
                throw new Exception($"Database migrator upgraded from {oldVersion} to {newVersion} (app's version is {desiredVersion}).");
        }

        static void validateMigrator(int oldVersion, DatabaseMigrator migrator, int desiredVersion)
        {
            if (migrator == null)
                throw new Exception($"Do not know how to migrate database version {oldVersion} (app's version is {desiredVersion})");

            if (migrator.OldVersion != oldVersion)
                throw new Exception($"received wrong database migrator");
        }
    }

}