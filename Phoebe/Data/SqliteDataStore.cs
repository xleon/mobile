using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Async;
using SQLite.Net.Interop;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class SqliteDataStore : IDataStore
    {
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
            DatabaseCleanUp ();
        }

        internal static IEnumerable<Type> DiscoverDataObjectTypes ()
        {
            var dataType = typeof (TimeEntryData);
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

        private void DatabaseCleanUp ()
        {
            // TODO: temporal method to clear old
            // draft entries from DB. It should be removed
            // in next versions.
            cnn.Table <TimeEntryData> ().Delete (t => t.State == TimeEntryState.New);

            // TODO: temporal method to clear
            // data with wrong workspace defined.
            var user = cnn.Table <UserData> ().FirstOrDefault ();
            if (user != null && user.DefaultWorkspaceId != Guid.Empty) {
                var tableNames = new List<string>  { cnn.Table<TimeEntryData>().Table.TableName,
                                                     cnn.Table<ClientData>().Table.TableName,
                                                     cnn.Table<ProjectData>().Table.TableName,
                                                     cnn.Table<TagData>().Table.TableName,
                                                     cnn.Table<TaskData>().Table.TableName
                                                   };
                cnn.RunInTransaction (() =>  {
                    foreach (var tableName in tableNames) {
                        var q = string.Concat ("UPDATE ", tableName ," SET WorkspaceId = '", user.DefaultWorkspaceId ,"' WHERE WorkspaceId = '", Guid.Empty, "'");
                        cnn.Execute (q);
                    }
                });
            }
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
            // TODO: Patch for release 8.1.2 to avoid empty workspaces
            if (Context.DetectEmptyWorkspaces<T> (obj)) {
                obj = Context.FixEmptyWorkspaces<T> (cnn, obj);
            }

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

        public async Task ExecuteInTransactionAsync (Action<IDataStoreContext>  worker)
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
            where T : class, new()
            {
                // TODO: Patch for release 8.1.2 to avoid empty workspaces
                if (DetectEmptyWorkspaces<T> (obj)) {
                    obj = FixEmptyWorkspaces<T> (conn, obj);
                }

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

            public static bool DetectEmptyWorkspaces<T> (T obj)
            {
                if (obj is TimeEntryData) {
                    var t = obj as TimeEntryData;
                    return t.WorkspaceId == Guid.Empty;
                } else if (obj is ProjectData) {
                    var t = obj as ProjectData;
                    return t.WorkspaceId == Guid.Empty;
                } else if (obj is ClientData) {
                    var t = obj as ClientData;
                    return t.WorkspaceId == Guid.Empty;
                } else if (obj is TaskData) {
                    var t = obj as TaskData;
                    return t.WorkspaceId == Guid.Empty;
                } else if (obj is TagData) {
                    var t = obj as TagData;
                    return t.WorkspaceId == Guid.Empty;
                }
                return false;
            }

            public static T FixEmptyWorkspaces<T> (SQLiteConnection cnn, T obj) where T : class, new()
            {
                // TODO: temporal method to clear
                // data with wrong workspace defined.
                var user = cnn.Table <UserData> ().FirstOrDefault ();

                if (user != null && user.DefaultWorkspaceId != Guid.Empty) {
                    if (obj is TimeEntryData) {
                        var t = obj as TimeEntryData;
                        t.WorkspaceId = user.DefaultWorkspaceId;
                        return t as T;
                    } else if (obj is ProjectData) {
                        var t = obj as ProjectData;
                        t.WorkspaceId = user.DefaultWorkspaceId;
                        return t as T;
                    } else if (obj is ClientData) {
                        var t = obj as ClientData;
                        t.WorkspaceId = user.DefaultWorkspaceId;
                        return t as T;
                    } else if (obj is TaskData) {
                        var t = obj as TaskData;
                        t.WorkspaceId = user.DefaultWorkspaceId;
                        return t as T;
                    } else if (obj is TagData) {
                        var t = obj as TagData;
                        t.WorkspaceId = user.DefaultWorkspaceId;
                        return t as T;
                    }
                }

                return obj;
            }

            public SQLiteConnection Connection
            {
                get { return conn; }
            }
        }
    }
}
