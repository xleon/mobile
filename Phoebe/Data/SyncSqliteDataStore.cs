using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite.Net;
using SQLite.Net.Interop;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data
{
    public class SyncSqliteDataStore : ISyncDataStore
    {
        public const int DB_VERSION = 1;

        public class MetaData
        {
            [SQLite.Net.Attributes.PrimaryKey]
            public string Id { get; set; }
            public string Json { get; set; }

            public T Convert<T> ()
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (Json);
            }

            public static MetaData Create<T> (string id, T data)
            {
                return new MetaData
                {
                    Id = id,
                    Json = Newtonsoft.Json.JsonConvert.SerializeObject(data)
                };
            }
        }

        readonly SQLiteConnectionWithLock cnn;

        public SyncSqliteDataStore(string dbPath, ISQLitePlatform platformInfo)
        {
            this.cnn = this.initDatabaseConnection(dbPath, platformInfo);

            CleanOldDraftEntry();
        }

        private SQLiteConnectionWithLock initDatabaseConnection(string dbPath, ISQLitePlatform platformInfo)
        {
            var dbFileExisted = File.Exists(dbPath) && new FileInfo(dbPath).Length > 0;

            var cnnString = new SQLiteConnectionString(dbPath, true);
            var connection = new SQLiteConnectionWithLock(platformInfo, cnnString);

            if (dbFileExisted)
            {
                var version = DatabaseHelper.GetVersion(connection);
                if (version != DB_VERSION)
                {
                    DatabaseHelper.Migrate(connection, platformInfo, dbPath, DB_VERSION);
                    connection = new SQLiteConnectionWithLock(platformInfo, cnnString);
                }
            }
            else
            {
                CreateTables(connection);
            }

            return connection;
        }

        public int GetVersion()
        {
            return DatabaseHelper.GetVersion(this.cnn);
        }

        private static void CreateTables(SQLiteConnection connection)
        {
            // TODO: the migration logic in principle knows what tables are needed
            // and how to set the version number. they could be used to get rid of
            // this method and of GetDataModels() below

            // Meta Data: DB Version, etc
            connection.CreateTable<MetaData> ();
            connection.InsertOrIgnore(MetaData.Create(nameof(DB_VERSION), DB_VERSION));

            // Data Models: Time Entries, etc
            foreach (var t in GetDataModels())
            {
                connection.CreateTable(t);
            }
        }

        private void CleanOldDraftEntry()
        {
            // TODO: temporal method to clear old draft entries from DB.
            // It should be removed in next versions.
            cnn.Table<TimeEntryData>().Delete(t => t.State == TimeEntryState.New);
        }

        internal static List<Type> GetDataModels()
        {
            return new List<Type>
            {
                typeof(UserData),
                typeof(WorkspaceData),
                typeof(WorkspaceUserData),
                typeof(ProjectData),
                typeof(ProjectUserData),
                typeof(ClientData),
                typeof(TaskData),
                typeof(TagData),
                typeof(TimeEntryData)
            };
        }

        public TableQuery<T> Table<T>() where T : CommonData, new()
        {
            return cnn.Table<T>();
        }

        public IReadOnlyList<ICommonData> Update(Action<ISyncDataStoreContext> worker)
        {
            IReadOnlyList<ICommonData> updated = null;

            cnn.RunInTransaction(() =>
            {
                var ctx = new SyncSqliteDataStoreContext(cnn);
                worker(ctx);
                updated = ctx.UpdatedItems;
            });

            return updated;
        }

        public void WipeTables()
        {
            var dataTypes = GetDataModels();
            dataTypes.Add(typeof(MetaData));

            foreach (var t in dataTypes)
            {
                var map = cnn.GetMapping(t);
                var query = string.Format("DELETE FROM \"{0}\"", map.TableName);
                cnn.Execute(query);
            }
        }

        #region Queue
        const string QueueCreateSql = "CREATE TABLE IF NOT EXISTS [__QUEUE__{0}] (Data TEXT)";
        const string QueueInsertSql = "INSERT INTO [__QUEUE__{0}] VALUES (?)";
        const string QueueSelectFirstSql = "SELECT rowid, * FROM [__QUEUE__{0}] ORDER BY rowid LIMIT 1";
        const string QueueDeleteSql = "DELETE FROM [__QUEUE__{0}] WHERE rowid = ?";
        const string QueueResetSql = "DELETE FROM [__QUEUE__{0}]";
        const string QueueCountSql = "SELECT COUNT(*) FROM [__QUEUE__{0}]";

        class QueueItem
        {
            [SQLite.Net.Attributes.Column("rowid")]
            public long RowId { get; set; }
            public string Data { get; set; }
        }

        private void CreateQueueTable(string queueId)
        {
            cnn.Execute(string.Format(QueueCreateSql, queueId));
        }

        public int ResetQueue(string queueId)
        {
            CreateQueueTable(queueId);
            return cnn.ExecuteScalar<int> (string.Format(QueueResetSql, queueId));
        }

        public int GetQueueSize(string queueId)
        {
            CreateQueueTable(queueId);
            return cnn.ExecuteScalar<int> (string.Format(QueueCountSql, queueId));
        }

        public bool TryEnqueue(string queueId, string json)
        {
            CreateQueueTable(queueId);
            var res = cnn.Execute(string.Format(QueueInsertSql, queueId), json);
            return res == 1;
        }

        public bool TryDequeue(string queueId, out string json)
        {
            json = null;
            CreateQueueTable(queueId);

            var cmd = cnn.CreateCommand(string.Format(QueueSelectFirstSql, queueId));
            var record = cmd.ExecuteQuery<QueueItem>().SingleOrDefault();
            if (record != null)
            {
                cmd = cnn.CreateCommand(string.Format(QueueDeleteSql, queueId), record.RowId);
                var res = cmd.ExecuteNonQuery();
                if (res != 1)
                {
                    return false;
                }
                else
                {
                    json = record.Data;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public bool TryPeek(string queueId, out string json)
        {
            CreateQueueTable(queueId);

            var cmd = cnn.CreateCommand(string.Format(QueueSelectFirstSql, queueId));
            var record = cmd.ExecuteQuery<QueueItem>().SingleOrDefault();
            if (record != null)
            {
                json = record.Data;
                return true;
            }
            else
            {
                json = null;
                return false;
            }
        }
        #endregion

        public class SyncSqliteDataStoreContext : ISyncDataStoreContext
        {
            readonly SQLiteConnection conn;
            readonly List<ICommonData> updated;

            public SyncSqliteDataStoreContext(SQLiteConnection conn)
            {
                this.conn = conn;
                this.updated = new List<ICommonData>();
            }

            public SQLiteConnection Connection
            {
                get { return conn; }
            }

            public IReadOnlyList<ICommonData> UpdatedItems
            {
                get { return updated; }
            }

            public void Put(ICommonData obj)
            {
                var success = conn.InsertOrReplace(obj) == 1;
                if (success)
                {
                    updated.Add(obj);
                }
            }

            public void Delete(ICommonData obj)
            {
                var success = conn.Delete(obj) == 1;
                if (success)
                {
                    updated.Add(obj);
                }
            }

            // TODO: RX Find an elegant way to
            // replace this method.
            // Paul found a possible solution when looking at this:
            // double dispatch/visitor pattern. (feel free to ask for details/implementation)
            // (if such a 'complex' solution is needed at all.
            // this method is only used in one place at the time of writing)
            public ICommonData GetByColumn(Type type, string colName, object colValue)
            {
                IEnumerable<ICommonData> res;
                var map = conn.GetMapping(type);
                var query = $"SELECT * FROM [{map.TableName}] WHERE {colName}=?";

                if (type == typeof(ClientData))
                {
                    res = conn.Query<ClientData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(ProjectData))
                {
                    res = conn.Query<ProjectData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(TaskData))
                {
                    res = conn.Query<TaskData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(TimeEntryData))
                {
                    res = conn.Query<TimeEntryData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(WorkspaceData))
                {
                    res = conn.Query<WorkspaceData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(UserData))
                {
                    res = conn.Query<UserData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(TagData))
                {
                    res = conn.Query<TagData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(WorkspaceUserData))
                {
                    res = conn.Query<WorkspaceUserData> (query, colValue).Cast<ICommonData>();
                }
                else if (type == typeof(ProjectUserData))
                {
                    res = conn.Query<ProjectUserData> (query, colValue).Cast<ICommonData>();
                }
                else
                {
                    throw new NotSupportedException(string.Format("Cannot find table for {0}", type.Name));
                }

                return res.SingleOrDefault();
            }
        }
    }
}

