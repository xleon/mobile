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
        private readonly SQLiteAsyncConnection sqliteAsyncConnection;
        private readonly Context ctx;
        #pragma warning disable 0414
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
        #pragma warning restore 0414

        public SqliteDataStore (string dbPath, ISQLitePlatform platformInfo)
        {
            // When using this constructor, Context will create a connection with lock
            // so we can safely make the cast
            ctx = new Context (this, dbPath, platformInfo);
            sqliteAsyncConnection = new SQLiteAsyncConnection (() =>
                ctx.Connection as SQLiteConnectionWithLock);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);

            CreateTables ();
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
                ctx.Connection.CreateTable (t);
            }
        }

        private void WipeTables ()
        {
            var dataObjects = DiscoverDataObjectTypes ();

            foreach (var t in dataObjects) {
                var map = ctx.Connection.GetMapping (t);
                var query = string.Format ("DELETE FROM \"{0}\"", map.TableName);
                ctx.Connection.Execute (query);
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

        public async Task<T> PutAsync<T> (T obj) where T : class, new()
        {
            obj = Clone (obj);
            try {
                // TODO: need some checking like in DeleteAsync
                await sqliteAsyncConnection.InsertOrReplaceAsync (obj);
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Send (new DataChangeMessage (this, obj, DataAction.Put));
                return obj;
            } finally {
                ctx.SendMessages ();
            }
        }

        public async Task<bool> DeleteAsync (object obj)
        {
            obj = Clone (obj);
            try {
                var result = await sqliteAsyncConnection.DeleteAsync (obj);
                if (result == 1) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    bus.Send (new DataChangeMessage (this, obj, DataAction.Delete));
                }
                return result == 1;
            } finally {
                ctx.SendMessages ();
            }
        }

        public Task<T> ExecuteScalarAsync<T> (string query, params object[] args)
        {
            return sqliteAsyncConnection.ExecuteScalarAsync<T> (query, args);
        }

        public Task<List<T>> QueryAsync<T> (string query, params object[] args) where T : class, new()
        {
            return sqliteAsyncConnection.QueryAsync<T> (query, args);
        }

        public AsyncTableQuery<T> Table<T> () where T : class, new()
        {
            return sqliteAsyncConnection.Table<T> ();
        }

        public async Task<string> GetTableNameAsync<T> ()
        {
            var mapping = await sqliteAsyncConnection.GetMappingAsync<T> ();
            return mapping == null ? null : mapping.TableName;
        }

        public async Task<T> ExecuteInTransactionAsync<T> (Func<IDataStoreContext, T> worker)
        {
            var result = default (T);
            try {
                Context tempCtx = null;
                await sqliteAsyncConnection.RunInTransactionAsync (conn => {
                    tempCtx = new Context(this, conn);
                    result = worker (tempCtx);
                });
                tempCtx.SendMessages ();
                return result;
            } catch {
                // The temp context gets discarded, we don't need to clear the messages
                //                ctx.ClearMessages ();
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync (Action<IDataStoreContext> worker)
        {
            try {
                Context tempCtx = null;
                await sqliteAsyncConnection.RunInTransactionAsync (conn => {
                    tempCtx = new Context(this, conn);
                    worker (tempCtx);
                });
                tempCtx.SendMessages ();
            } catch {
                throw;
            }
        }

        private class Context : IDataStoreContext
        {
            private readonly List<DataChangeMessage> messages = new List<DataChangeMessage> ();
            private readonly SqliteDataStore store;
            private readonly SQLiteConnection conn;

            public Context (SqliteDataStore store, string dbPath, ISQLitePlatform platformInfo)
            {
                var connectionString = new SQLiteConnectionString (dbPath, true);
                this.store = store;
                conn = new SQLiteConnectionWithLock (platformInfo, connectionString);
            }

            public Context (SqliteDataStore store, SQLiteConnection conn)
            {
                this.store = store;
                this.conn = conn;
            }

            public T Put<T> (T obj)
                where T : new()
            {
                conn.InsertOrReplace (obj);

                // Schedule message to be sent about this update post transaction
                messages.Add (new DataChangeMessage (store, obj, DataAction.Put));

                return obj;
            }

            public bool Delete (object obj)
            {
                var count = conn.Delete (obj);
                var success = count > 0;

                // Schedule message to be sent about this delete post transaction
                if (success) {
                    messages.Add (new DataChangeMessage (store, obj, DataAction.Delete));
                }

                return success;
            }

            public SQLiteConnection Connection
            {
                get { return conn; }
            }

            public void SendMessages ()
            {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                foreach (var msg in messages) {
                    bus.Send (msg);
                }
                messages.Clear ();
            }

            public void ClearMessages ()
            {
                messages.Clear ();
            }
        }

    }
}
