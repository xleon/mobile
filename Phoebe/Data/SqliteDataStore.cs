using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Async;
using SQLite.Net.Interop;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class SqliteDataStore : IDataStore
    {
        private const string QueueCreateSql = "CREATE TABLE IF NOT EXISTS [__QUEUE__] (DATA TEXT)";
        private const string QueueInsertSql = "INSERT INTO [__QUEUE__] VALUES (?)";
        private const string QueueSelectFirstSql = "SELECT ROWID, DATA FROM [__QUEUE__] ORDER BY ROWID LIMIT 1";
        private const string QueueDeleteSql = "DELETE FROM [__QUEUE__] WHERE ROWID = ?";

        private readonly SQLiteConnectionWithLock cnn;
#pragma warning disable 0414
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
#pragma warning restore 0414

        public SqliteDataStore (string dbPath, ISQLitePlatform platformInfo)
        {
            // When using this constructor, Context will create a connection with lock
            // so we can safely make the cast
            var cnnString = new SQLiteConnectionString (dbPath, true);
            cnn = new SQLiteConnectionWithLock (platformInfo, cnnString);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);

            CreateTables();
            CreateQueueTable ();
            CleanOldDraftEntry ();
        }

        internal static IEnumerable<Type> DiscoverDataObjectTypes ()
        {
            var dataType = typeof (Toggl.Phoebe.Data.DataObjects.TimeEntryData);
            return from t in dataType.Assembly.GetTypes ()
                   where t.Namespace == dataType.Namespace && !t.IsAbstract
                   select t;
        }

        private void CreateTables ()
        {
            var dataObjects = DiscoverDataObjectTypes ();
            foreach (var t in dataObjects) {
                cnn.CreateTable (t);
            }
        }

        private void CreateQueueTable ()
        {
            cnn.Execute (QueueCreateSql);
        }

        private void CleanOldDraftEntry ()
        {
            // TODO: temporal method to clear old
            // draft entries from DB. It should be removed
            // in next versions.
            cnn.Table <Toggl.Phoebe.Data.DataObjects.TimeEntryData> ().Delete (t => t.State == TimeEntryState.New);
        }

        private void WipeTables ()
        {
            var dataObjects = DiscoverDataObjectTypes ();

            foreach (var t in dataObjects) {
                var map = cnn.GetMapping (t);
                var query = string.Format ("DELETE FROM \"{0}\"", map.TableName);
                cnn.Execute (query);
            }
        }

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            if (msg.AuthManager.IsAuthenticated) {
                return;
            }

            // Wipe database on logout
            WipeTables ();
        }

        private T Clone<T> (T obj)
        {
            var type = obj.GetType ();

            try {
                // Try to copy the object using copy constructor
                return (T)Activator.CreateInstance (type, new[] { obj });
            } catch (MissingMethodException) {
            }

            // Clone the object using reflection
            var other = (T)Activator.CreateInstance (type);
            foreach (var prop in type.GetProperties()) {
                prop.SetValue (other, prop.GetValue (obj));
            }
            return other;
        }

        private void SendMessage (DataChangeMessage message)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (message);
        }

        private void SendMessages (IList<DataChangeMessage> messages)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            foreach (var msg in messages) {
                bus.Send (msg);
            }
        }

        private SQLiteAsyncConnection CreateAsyncCnn()
        {
            return new SQLiteAsyncConnection (() => cnn);
        }

        public async Task<T> PutAsync<T> (T obj) where T : class, new()
        {
            obj = Clone (obj);
            var success = await CreateAsyncCnn().InsertOrReplaceAsync (obj) == 1;
            if (success) {
                SendMessage (new DataChangeMessage (this, obj, DataAction.Put));
            }
            return obj;
        }

        public async Task<bool> DeleteAsync (object obj)
        {
            obj = Clone (obj);
            var success = await CreateAsyncCnn().DeleteAsync (obj) == 1;
            if (success) {
                SendMessage (new DataChangeMessage (this, obj, DataAction.Delete));
            }
            return success;
        }

        public Task<T> ExecuteScalarAsync<T> (string query, params object[] args)
        {
            return CreateAsyncCnn().ExecuteScalarAsync<T> (query, args);
        }

        public Task<List<T>> QueryAsync<T> (string query, params object[] args) where T : class, new()
        {
            return CreateAsyncCnn().QueryAsync<T> (query, args);
        }

        public AsyncTableQuery<T> Table<T> () where T : class, new()
        {
            return CreateAsyncCnn().Table<T> ();
        }

        public async Task<string> GetTableNameAsync<T> ()
        {
            var mapping = await CreateAsyncCnn().GetMappingAsync<T> ();
            return mapping == null ? null : mapping.TableName;
        }

        public async Task<T> ExecuteInTransactionAsync<T> (Func<IDataStoreContext, T> worker)
        {
            var result = default (T);
            try {
                Context ctx = null;
                await CreateAsyncCnn().RunInTransactionAsync (conn => {
                    ctx = new Context (this, conn);
                    result = worker (ctx);
                });
                SendMessages (ctx.Messages);
                return result;
            } catch {
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync (Action<IDataStoreContext> worker)
        {
            try {
                Context ctx = null;
                await CreateAsyncCnn().RunInTransactionAsync (conn => {
                    ctx = new Context (this, conn);
                    worker (ctx);
                });
                SendMessages (ctx.Messages);
            } catch {
                throw;
            }
        }

        // TODO: Temporary
        public async Task<DataChangeMessage[]> ExecuteInTransactionWithMessagesAsync (
            Action<IDataStoreContext> worker)
        {
            Context ctx = null;
            await CreateAsyncCnn().RunInTransactionAsync (conn => {
                ctx = new Context (this, conn);
                worker (ctx);
            });

            return ctx.Messages.ToArray ();
        }

        private class Context : IDataStoreContext
        {
            class QueueItem
            {
                public long RowId { get; set; }
                public string Data { get; set; }
            }

            private readonly SqliteDataStore store;
            private readonly SQLiteConnection conn;

            public List<DataChangeMessage> Messages { get; private set; }

            public Context (SqliteDataStore store, SQLiteConnection conn)
            {
                this.store = store;
                this.conn = conn;
                Messages =  new List<DataChangeMessage> ();
            }

            public T Put<T> (T obj)
            where T : new()
            {
                var success = conn.InsertOrReplace (obj) == 1;
                if (success) {
                    // Schedule message to be sent about this update post transaction
                    Messages.Add (new DataChangeMessage (store, obj, DataAction.Put));
                }
                return obj;
            }

            public bool Delete (object obj)
            {
                var success = conn.Delete (obj) == 1;
                if (success) {
                    // Schedule message to be sent about this delete post transaction
                    Messages.Add (new DataChangeMessage (store, obj, DataAction.Delete));
                }
                return success;
            }

            public SQLiteConnection Connection
            {
                get { return conn; }
            }

            public void Enqueue (string json)
            {
                conn.Execute (QueueInsertSql, json);
            }

            public bool TryDequeue (out string json)
            {
                var cmd = conn.CreateCommand (QueueSelectFirstSql);
                var record = cmd.ExecuteQuery<QueueItem> ().Single ();
                if (record != null) {
                    cmd = conn.CreateCommand (QueueDeleteSql, record.RowId);
                    cmd.ExecuteNonQuery ();
                    json = record.Data;
                    return true;
                }
                else {
                    json = null;
                    return false;
                }
            }

            public bool TryPeekQueue (out string json)
            {
                var cmd = conn.CreateCommand (QueueSelectFirstSql);
                var record = cmd.ExecuteQuery<QueueItem> ().Single ();
                if (record != null) {
                    json = record.Data;
                    return true;
                }
                else {
                    json = null;
                    return false;
                }
            }
        }
    }
}
