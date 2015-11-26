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
        private readonly Context ctx;
#pragma warning disable 0414
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
#pragma warning restore 0414

        public SqliteDataStore (string dbPath, ISQLitePlatform platformInfo)
        {
            ctx = new Context (this, dbPath, platformInfo);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);

            CreateTables ().RunSynchronously();
            //Task.Run (async () => await CreateTables ());
        }

        internal static IEnumerable<Type> DiscoverDataObjectTypes ()
        {
            var dataType = typeof (Toggl.Phoebe.Data.DataObjects.TimeEntryData);
            return from t in dataType.Assembly.GetTypes ()
                   where t.Namespace == dataType.Namespace && !t.IsAbstract
                   select t;
        }

        private async Task CreateTables ()
        {
            var dataObjects = DiscoverDataObjectTypes ();
            await ctx.Connection.CreateTablesAsync (dataObjects.ToArray());
        }

        private async Task WipeTables ()
        {
            var dataObjects = DiscoverDataObjectTypes ();
            await ctx.Connection.RunInTransactionAsync (trans => {
                foreach (var t in dataObjects) {
                    var map = trans.GetMapping (t);
                    var query = string.Format ("DELETE FROM \"{0}\"", map.TableName);
                    trans.Execute (query);
                }
            });
        }

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            if (msg.AuthManager.IsAuthenticated) {
                return;
            }

            // Wipe database on logout
            WipeTables ().RunSynchronously();
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

        public Task<T> PutAsync<T> (T obj)
        where T : class, new()
        {
            obj = Clone (obj);
            try {
                return ctx.PutAsync<T> (obj);
            } finally {
                ctx.SendMessages ();
            }
        }

        public Task<bool> DeleteAsync (object obj)
        {
            obj = Clone (obj);
            try {
                return ctx.DeleteAsync (obj);
            } finally {
                ctx.SendMessages ();
            }
        }

        public async Task<T> ExecuteScalarAsync<T> (string query, params object[] args)
        {
            return await ctx.Connection.ExecuteScalarAsync<T> (query, args);
        }

        public async Task<List<T>> QueryAsync<T> (string query, params object[] args) where T : class, new()
        {
            return await ctx.Connection.QueryAsync<T> (query, args);
        }

        public AsyncTableQuery<T> Table<T> () where T : class, new()
        {
            return ctx.Connection.Table<T> ();
        }

        public async Task<string> GetTableNameAsync<T> ()
        {
            var mapping = await ctx.Connection.GetMappingAsync<T> ();
            return mapping == null ? null : mapping.TableName;
        }

        public async Task<T> ExecuteInTransactionAsync<T> (Func<IDataStoreContextSync, T> worker)
        {
            var result = default (T);
            try {
                await ctx.Connection.RunInTransactionAsync (trans => {
                    result = worker (new ContextSync (ctx, trans));
                });
                ctx.SendMessages ();
                return result;
            } catch {
                ctx.ClearMessages ();
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync (Action<IDataStoreContextSync> worker)
        {
            try {
                await ctx.Connection.RunInTransactionAsync (trans => worker (new ContextSync (ctx, trans)));
                ctx.SendMessages ();
            } catch {
                ctx.ClearMessages ();
                throw;
            }
        }

        private class Context : IDataStoreContext
        {
            private readonly List<DataChangeMessage> messages = new List<DataChangeMessage> ();
            private readonly SqliteDataStore store;
            private readonly SQLiteConnectionWithLock conn;

            public Context (SqliteDataStore store, string dbPath, ISQLitePlatform platformInfo)
            {
                var connectionString = new SQLiteConnectionString (dbPath, true);
                this.store = store;
                conn = new SQLiteConnectionWithLock (platformInfo, connectionString);
            }

            public async Task<T> PutAsync<T> (T obj) where T : new()
            {
                var cnn = new SQLiteAsyncConnection (() => conn);
                await cnn.InsertOrReplaceAsync (obj);

                // Schedule message to be sent about this update post transaction
                messages.Add (new DataChangeMessage (store, obj, DataAction.Put));

                return obj;
            }

            public async Task<bool> DeleteAsync (object obj)
            {
                var cnn = new SQLiteAsyncConnection (() => conn);
                var count = await cnn.DeleteAsync (obj);
                var success = count > 0;

                // Schedule message to be sent about this delete post transaction
                if (success) {
                    messages.Add (new DataChangeMessage (store, obj, DataAction.Delete));
                }

                return success;
            }

            public SQLiteAsyncConnection Connection
            {
                get { return new SQLiteAsyncConnection (() => conn); }
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

            public void AddMessage (object data, DataAction action)
            {
                messages.Add (new DataChangeMessage (store, data, action));
            }
        }

        private class ContextSync : IDataStoreContextSync
        {
            private readonly Context parent;
            private readonly SQLiteConnection conn;

            public ContextSync (Context parent, SQLiteConnection conn)
            {
                this.parent = parent;
                this.conn = conn;
            }

            public SQLiteConnection Connection
            {
                get { return conn; }
            }

            public T Put<T> (T obj) where T : new()
            {
                conn.InsertOrReplace (obj);

                // Schedule message to be sent about this update post transaction
                parent.AddMessage (obj, DataAction.Put);

                return obj;
            }

            public bool Delete (object obj)
            {
                var count = conn.Delete (obj);
                var success = count > 0;

                // Schedule message to be sent about this delete post transaction
                if (success) {
                    parent.AddMessage (obj, DataAction.Delete);
                }

                return success;
            }
        }
    }
}
