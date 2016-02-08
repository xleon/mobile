using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Net;
using SQLite.Net.Interop;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Data
{
    public class SyncSqliteDataStore 
    {

        readonly SQLiteConnectionWithLock cnn;
        readonly ISQLitePlatform platformInfo;

        public SyncSqliteDataStore (string dbPath, ISQLitePlatform platformInfo)
        {
            var cnnString = new SQLiteConnectionString (dbPath, true);
            this.cnn = new SQLiteConnectionWithLock (platformInfo, cnnString);
            this.platformInfo = platformInfo;

            CreateTables();
            CleanOldDraftEntry ();
        }

        private void CreateTables ()
        {
            var dataObjects = DiscoverDataObjectTypes ();
            foreach (var t in dataObjects) {
                cnn.CreateTable (t);
            }
        }

        private void CleanOldDraftEntry ()
        {
            // TODO: temporal method to clear old draft entries from DB.
            // It should be removed in next versions.
            cnn.Table <TimeEntryData> ().Delete (t => t.State == TimeEntryState.New);
        }

        internal static IEnumerable<Type> DiscoverDataObjectTypes ()
        {
            var dataType = typeof (TimeEntryData);
            return from t in dataType.Assembly.GetTypes ()
                    where t.Namespace == dataType.Namespace && !t.IsAbstract
                select t;
        }

        public void ExecuteInTransaction (Action<ISyncDataStoreContext> worker)
        {
            cnn.RunInTransaction (() => {
                worker(new SyncSqliteDataStoreContext(cnn));
            });
        }

        public class SyncSqliteDataStoreContext : ISyncDataStoreContext
        {
            readonly SQLiteConnectionWithLock conn;

            public SyncSqliteDataStoreContext(SQLiteConnectionWithLock conn)
            {
                this.conn = conn;
            }

            public bool Put (object obj)
            {
                var success = conn.InsertOrReplace (obj) == 1;
                return success;
            }

            public bool Delete (object obj)
            {
                var success = conn.Delete (obj) == 1;
                return success;
            }

            #region Queue methods
            const string QueueCreateSql = "CREATE TABLE IF NOT EXISTS [__QUEUE__{0}] (Data TEXT)";
            const string QueueInsertSql = "INSERT INTO [__QUEUE__{0}] VALUES (?)";
            const string QueueSelectFirstSql = "SELECT rowid, * FROM [__QUEUE__{0}] ORDER BY rowid LIMIT 1";
            const string QueueDeleteSql = "DELETE FROM [__QUEUE__{0}] WHERE rowid = ?";
            const string QueueCountSql = "SELECT COUNT(*) FROM [__QUEUE__{0}]";

            class QueueItem
            {
                [SQLite.Net.Attributes.Column("rowid")]
                public long RowId { get; set; }
                public string Data { get; set; }
            }

            private void CreateQueueTable (string queueId)
            {
                conn.Execute (string.Format(QueueCreateSql, queueId));
            }

            public int GetQueueSize (string queueId)
            {
                CreateQueueTable (queueId);
                return conn.ExecuteScalar<int> (QueueCountSql);
            }

            public bool TryEnqueue (string queueId, string json)
            {
                CreateQueueTable (queueId);
                var res = conn.Execute (string.Format(QueueInsertSql, queueId), json);
                return res == 1;
            }

            public bool TryDequeue (string queueId, out string json)
            {
                json = null;
                CreateQueueTable (queueId);

                var cmd = conn.CreateCommand (string.Format(QueueSelectFirstSql, queueId));
                var record = cmd.ExecuteQuery<QueueItem> ().SingleOrDefault ();
                if (record != null) {
                    cmd = conn.CreateCommand (string.Format(QueueDeleteSql, queueId), record.RowId);
                    var res = cmd.ExecuteNonQuery ();
                    if (res != 1) {
                        return false;
                    }
                    else {
                        json = record.Data;
                        return true;
                    }   
                }
                else {
                    return false;
                }
            }

            public bool TryPeekQueue (string queueId, out string json)
            {
                CreateQueueTable (queueId);

                var cmd = conn.CreateCommand (string.Format(QueueSelectFirstSql, queueId));
                var record = cmd.ExecuteQuery<QueueItem> ().SingleOrDefault ();
                if (record != null) {
                    json = record.Data;
                    return true;
                }
                else {
                    json = null;
                    return false;
                }
            }
            #endregion
        }
    }
}

