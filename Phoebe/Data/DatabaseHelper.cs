using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SQLite.Net;
using SQLite.Net.Interop;
using Toggl.Phoebe.Logging;
using XPlatUtils;

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

        public static bool Migrate(SQLiteConnection connection, ISQLitePlatform platformInfo,
                                   string dbPath, int desiredVersion)
        {
            try
            {
                var migrateFromDB = connection;
                var version = GetVersion(migrateFromDB);

                var tempDBPath = dbPath + ".migrated";

                while (true)
                {
                    var migrator = DatabaseMigrator.ForVersion(version);

                    var expectedNewVersion = migrator.NewVersion;

                    validateMigrator(version, migrator, desiredVersion);

                    // Make sure the tempDBPath doesn't exist to prevent corruption of data
                    if (expectedNewVersion == desiredVersion && File.Exists(tempDBPath))
                        File.Delete(tempDBPath);

                    var newDB = new SQLiteConnection(platformInfo, expectedNewVersion == desiredVersion
                                                     ? tempDBPath
                                                     : "Data Source=:memory:");

                    migrator.Migrate(migrateFromDB, newDB);
                    migrateFromDB.Close();

                    var newVersion = GetVersion(newDB);

                    validateMigratedVersion(version, expectedNewVersion, newVersion, desiredVersion);

                    if (newVersion == desiredVersion)
                    {
                        newDB.Close();
                        replaceDatabase(dbPath, tempDBPath);
                        return true;
                    }

                    migrateFromDB = newDB;
                }
            }
            catch (MigrationException ex)
            {
                var logger = ServiceContainer.Resolve<ILogger>();
                logger.Error(nameof(DatabaseMigrator), ex, ex.Message);
            }
            catch (Exception ex)
            {
                var ex2 = new MigrationException("Unknown exception during migration", ex);
                var logger = ServiceContainer.Resolve<ILogger>();
                logger.Error(nameof(DatabaseMigrator), ex2, ex2.Message);
            }
            return false;
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
                throw new MigrationException($"Expected new database version {expectedNewVersion}, but was {newVersion}");

            if (newVersion <= oldVersion || newVersion > desiredVersion)
                throw new MigrationException($"Database migrator upgraded from {oldVersion} to {newVersion} (app's version is {desiredVersion}).");
        }

        static void validateMigrator(int oldVersion, DatabaseMigrator migrator, int desiredVersion)
        {
            if (migrator == null)
                throw new MigrationException($"Do not know how to migrate database version {oldVersion} (app's version is {desiredVersion})");

            if (migrator.OldVersion != oldVersion)
                throw new MigrationException($"received wrong database migrator");
        }
    }

}