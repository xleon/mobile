﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLite.Net;
using SQLite.Net.Interop;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Data
{
    public class SyncSqliteDataStore : ISyncDataStore
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
            var dataObjects = DiscoverDataModels ();
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

        internal static List<Type> DiscoverDataModels ()
        {
            return new List<Type> () {
                typeof (UserData),
                       typeof (WorkspaceData),
                       typeof (WorkspaceUserData),
                       typeof (ProjectData),
                       typeof (ProjectUserData),
                       typeof (ClientData),
                       typeof (TaskData),
                       typeof (TagData),
                       typeof (TimeEntryTagData),
                       typeof (TimeEntryData)
            };
        }

        public TableQuery<T> Table<T> () where T : CommonData, new()
        {
            return cnn.Table<T> ();
        }

        public IReadOnlyList<ICommonData> Update (Action<ISyncDataStoreContext> worker)
        {
            IReadOnlyList<ICommonData> updated = null;

            cnn.RunInTransaction (() => {
                var ctx = new SyncSqliteDataStoreContext (cnn);
                worker (ctx);
                updated = ctx.UpdatedItems;
            });

            return updated;
        }

        public void WipeTables ()
        {
            var dataObjects = DiscoverDataModels ();
            foreach (var t in dataObjects) {
                var map = cnn.GetMapping (t);
                var query = string.Format ("DELETE FROM \"{0}\"", map.TableName);
                cnn.Execute (query);
            }
        }

        #region Queue
        const string QueueCreateSql = "CREATE TABLE IF NOT EXISTS [__QUEUE__{0}] (Data TEXT)";
        const string QueueInsertSql = "INSERT INTO [__QUEUE__{0}] VALUES (?)";
        const string QueueSelectFirstSql = "SELECT rowid, * FROM [__QUEUE__{0}] ORDER BY rowid LIMIT 1";
        const string QueueDeleteSql = "DELETE FROM [__QUEUE__{0}] WHERE rowid = ?";
        const string QueueCountSql = "SELECT COUNT(*) FROM [__QUEUE__{0}]";

        class QueueItem
        {
            [SQLite.Net.Attributes.Column ("rowid")]
            public long RowId { get; set; }
            public string Data { get; set; }
        }

        private void CreateQueueTable (string queueId)
        {
            cnn.Execute (string.Format (QueueCreateSql, queueId));
        }

        public int GetQueueSize (string queueId)
        {
            CreateQueueTable (queueId);
            return cnn.ExecuteScalar<int> (string.Format (QueueCountSql, queueId));
        }

        public bool TryEnqueue (string queueId, string json)
        {
            CreateQueueTable (queueId);
            var res = cnn.Execute (string.Format (QueueInsertSql, queueId), json);
            return res == 1;
        }

        public bool TryDequeue (string queueId, out string json)
        {
            json = null;
            CreateQueueTable (queueId);

            var cmd = cnn.CreateCommand (string.Format (QueueSelectFirstSql, queueId));
            var record = cmd.ExecuteQuery<QueueItem> ().SingleOrDefault ();
            if (record != null) {
                cmd = cnn.CreateCommand (string.Format (QueueDeleteSql, queueId), record.RowId);
                var res = cmd.ExecuteNonQuery ();
                if (res != 1) {
                    return false;
                } else {
                    json = record.Data;
                    return true;
                }
            } else {
                return false;
            }
        }

        public bool TryPeek (string queueId, out string json)
        {
            CreateQueueTable (queueId);

            var cmd = cnn.CreateCommand (string.Format (QueueSelectFirstSql, queueId));
            var record = cmd.ExecuteQuery<QueueItem> ().SingleOrDefault ();
            if (record != null) {
                json = record.Data;
                return true;
            } else {
                json = null;
                return false;
            }
        }
        #endregion

        public class SyncSqliteDataStoreContext : ISyncDataStoreContext
        {
            readonly SQLiteConnectionWithLock conn;
            readonly List<ICommonData> updated;

            public SyncSqliteDataStoreContext (SQLiteConnectionWithLock conn)
            {
                this.conn = conn;
                this.updated = new List<ICommonData> ();
            }

            public SQLiteConnection Connection
            {
                get { return conn; }
            }

            public IReadOnlyList<ICommonData> UpdatedItems
            {
                get { return updated; }
            }

            public void Put (ICommonData obj)
            {
                var success = conn.InsertOrReplace (obj) == 1;
                if (success) {
                    updated.Add (obj);
                }
            }

            public void Delete (ICommonData obj)
            {
                var success = conn.Delete (obj) == 1;
                if (success) {
                    updated.Add (obj);
                }
            }

            public ICommonData GetByColumn (Type type, string colName, object colValue)
            {
                IEnumerable<ICommonData> res;
                var map = conn.GetMapping (type);
                var query = $"SELECT * FROM [{map.TableName}] WHERE {colName}=?";

                if (type == typeof (ClientData)) {
                    res = conn.Query<ClientData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (ProjectData)) {
                    res = conn.Query<ProjectData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (TaskData)) {
                    res = conn.Query<TaskData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (TimeEntryData)) {
                    res = conn.Query<TimeEntryData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (WorkspaceData)) {
                    res = conn.Query<WorkspaceData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (UserData)) {
                    res = conn.Query<UserData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (TagData)) {
                    res = conn.Query<TagData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (WorkspaceUserData)) {
                    res = conn.Query<WorkspaceUserData> (query, colValue).Cast<ICommonData> ();
                } else if (type == typeof (ProjectUserData)) {
                    res = conn.Query<ProjectUserData> (query, colValue).Cast<ICommonData> ();
                } else {
                    throw new NotSupportedException (string.Format ("Cannot find table for {0}", type.Name));
                }

                return res.SingleOrDefault ();
            }
        }
    }
}
