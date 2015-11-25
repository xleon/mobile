using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Interop;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class SqliteDataStore : IDataStore
    {
        private readonly Scheduler scheduler = new Scheduler ();
        private readonly Context ctx;
#pragma warning disable 0414
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
#pragma warning restore 0414

        public SqliteDataStore (string dbPath, ISQLitePlatform platformInfo)
        {
            scheduler.Idle += HandleSchedulerIdle;
            ctx = new Context (this, dbPath, platformInfo);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);

            CreateTables ();
        }

        private void HandleSchedulerIdle (object sender, EventArgs args)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new DataStoreIdleMessage (this));
        }

        internal static IEnumerable<Type> DiscoverDataObjectTypes ()
        {
            var dataType = typeof (Toggl.Phoebe.Data.DataObjects.TimeEntryData);
            return from t in dataType.Assembly.GetTypes ()
                   where t.Namespace == dataType.Namespace && !t.IsAbstract
                   select t;
        }

        private async void CreateTables ()
        {
            await ExecuteInTransactionAsync (delegate {
                var dataObjects = DiscoverDataObjectTypes ();

                foreach (var t in dataObjects) {
                    ctx.Connection.CreateTable (t);
                }
            });
        }

        private async void WipeTables ()
        {
            await ExecuteInTransactionAsync (delegate {
                var dataObjects = DiscoverDataObjectTypes ();

                foreach (var t in dataObjects) {
                    var map = ctx.Connection.GetMapping (t);
                    var query = string.Format ("DELETE FROM \"{0}\"", map.TableName);
                    ctx.Connection.Execute (query);
                }
            });
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

        public Task<T> PutAsync<T> (T obj)
        where T : class, new()
        {
            obj = Clone (obj);
            return scheduler.Enqueue (delegate {
                try {
                    return ctx.Put<T> (obj);
                } finally {
                    ctx.SendMessages ();
                }
            });
        }

        public Task<bool> DeleteAsync (object obj)
        {
            obj = Clone (obj);
            return scheduler.Enqueue (delegate {
                try {
                    return ctx.Delete (obj);
                } finally {
                    ctx.SendMessages ();
                }
            });
        }

        public Task<T> ExecuteScalarAsync<T> (string query, params object[] args)
        {
            return scheduler.Enqueue (delegate {
                return ctx.Connection.ExecuteScalar<T> (query, args);
            });
        }

        public Task<List<T>> QueryAsync<T> (string query, params object[] args) where T : class, new()
        {
            return scheduler.Enqueue (delegate {
                return ctx.Connection.Query<T> (query, args);
            });
        }

        public IDataQuery<T> Table<T> () where T : class, new()
        {
            return new QueryBuilder<T> (this, ctx.Connection.Table<T> ());
        }

        public string GetTableName (Type mappingType)
        {
            var mapping = ctx.Connection.GetMapping (mappingType);
            if (mapping == null) {
                return null;
            }
            return mapping.TableName;
        }

        public Task<T> ExecuteInTransactionAsync<T> (Func<IDataStoreContext, T> worker)
        {
            return scheduler.Enqueue (delegate {
                T ret;

                ctx.Connection.BeginTransaction ();
                try {
                    ret = worker (ctx);
                    ctx.Connection.Commit ();
                    ctx.SendMessages ();
                } catch {
                    ctx.Connection.Rollback ();
                    ctx.ClearMessages ();
                    throw;
                }

                return ret;
            });
        }

        public Task ExecuteInTransactionAsync (Action<IDataStoreContext> worker)
        {
            return scheduler.Enqueue (delegate {
                ctx.Connection.BeginTransaction ();
                try {
                    worker (ctx);
                    ctx.Connection.Commit ();
                    ctx.SendMessages ();
                } catch {
                    ctx.Connection.Rollback ();
                    ctx.ClearMessages ();
                    throw;
                }
            });
        }

        private class QueryBuilder<T> : IDataQuery<T>
            where T : new()
        {
            private readonly SqliteDataStore store;
            private readonly TableQuery<T> query;

            public QueryBuilder (SqliteDataStore store, TableQuery<T> query) {
                this.store = store;
                this.query = query;
            }

            public IDataQuery<T> Where (Expression<Func<T, bool>> predicate) {
                return new QueryBuilder<T> (store, query.Where (predicate));
            }

            public IDataQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr, bool asc = true) {
                if (asc) {
                    return new QueryBuilder<T> (store, query.OrderBy (orderExpr));
                } else {
                    return new QueryBuilder<T> (store, query.OrderByDescending (orderExpr));
                }
            }

            public IDataQuery<T> Take (int n) {
                return new QueryBuilder<T> (store, query.Take (n));
            }

            public IDataQuery<T> Skip (int n) {
                return new QueryBuilder<T> (store, query.Skip (n));
            }

            public Task<int> CountAsync () {
                return store.scheduler.Enqueue (() => query.Count ());
            }

            public Task<int> CountAsync (Expression<Func<T, bool>> predicate) {
                return new QueryBuilder<T> (store, query.Where (predicate)).CountAsync ();
            }

            public Task<List<T>> QueryAsync () {
                return store.scheduler.Enqueue (() => query.ToList ());
            }

            public Task<List<T>> QueryAsync (Expression<Func<T, bool>> predicate) {
                return new QueryBuilder<T> (store, query.Where (predicate)).QueryAsync ();
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

        private class Scheduler
        {
            private const string Tag = "SQLiteDataStore.Scheduler";
            private readonly object syncRoot = new Object ();
            private readonly Queue<Action> workQueue = new Queue<Action> ();
            private bool isWorking;

            private async void EnsureProcessing ()
            {
                lock (syncRoot) {
                    if (isWorking) {
                        return;
                    }
                    isWorking = true;
                }

                try {
                    await Task.Factory.StartNew (ProcessQueue);
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<ILogger> ();
                    log.Error (Tag, ex, "Something exploded on the SQLite background thread.");
                } finally {
                    OnIdle ();
                }
            }

            private void ProcessQueue ()
            {
                while (true) {
                    Action act;

                    lock (syncRoot) {
                        if (workQueue.Count == 0) {
                            isWorking = false;
                            return;
                        }
                        act = workQueue.Dequeue ();
                    }

                    act ();
                }
            }

            private void OnIdle ()
            {
                var handler = Idle;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            }

            public event EventHandler Idle;

            public Task<T> Enqueue<T> (Func<T> task)
            {
                var tcs = new TaskCompletionSource<T> ();

                lock (syncRoot) {
                    workQueue.Enqueue (delegate {
                        try {
                            tcs.SetResult (task ());
                        } catch (Exception exc) {
                            tcs.SetException (exc);
                        }
                    });
                }

                EnsureProcessing ();

                return tcs.Task;
            }

            public Task Enqueue (Action task)
            {
                var tcs = new TaskCompletionSource<object> ();

                lock (syncRoot) {
                    workQueue.Enqueue (delegate {
                        try {
                            task ();
                            tcs.SetResult (null);
                        } catch (Exception exc) {
                            tcs.SetException (exc);
                        }
                    });
                }

                EnsureProcessing ();

                return tcs.Task;
            }
        }
    }
}
