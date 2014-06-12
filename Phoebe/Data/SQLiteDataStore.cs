using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class SQLiteDataStore : IDataStore
    {
        private readonly Scheduler scheduler = new Scheduler ();
        private readonly Context ctx;

        public SQLiteDataStore (string dbPath)
        {
            ctx = new Context (dbPath);
            CreateTables ();
        }

        private async void CreateTables ()
        {
            await ExecuteInTransactionAsync (delegate {
                // Discover data objects:
                var dataType = typeof(Toggl.Phoebe.Data.DataObjects.TimeEntryData);
                var dataObjects = from t in dataType.Assembly.GetTypes ()
                                              where t.Namespace == dataType.Namespace
                                              select t;

                // Create tables:
                foreach (var t in dataObjects) {
                    ctx.Connection.CreateTable (t);
                }
            });
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
            where T : new()
        {
            obj = Clone (obj);
            return scheduler.Enqueue (delegate {
                return ctx.Put<T> (obj);
            });
        }

        public Task<bool> DeleteAsync (object obj)
        {
            obj = Clone (obj);
            return scheduler.Enqueue (delegate {
                return ctx.Delete (obj);
            });
        }

        public Task<T> ExecuteScalarAsync<T> (string query, params object[] args)
        {
            return scheduler.Enqueue (delegate {
                return ctx.Connection.ExecuteScalar<T> (query, args);
            });
        }

        public Task<List<T>> QueryAsync<T> (string query, params object[] args) where T : new()
        {
            return scheduler.Enqueue (delegate {
                return ctx.Connection.Query<T> (query, args);
            });
        }

        public IDataQuery<T> Table<T> () where T : new()
        {
            throw new NotImplementedException ();
        }

        public Task<T> ExecuteInTransactionAsync<T> (Func<IDataStoreContext, T> worker)
        {
            return scheduler.Enqueue (delegate {
                T ret;

                ctx.Connection.BeginTransaction ();
                try {
                    ret = worker (ctx);
                    ctx.Connection.Commit ();
                } catch {
                    ctx.Connection.Rollback ();
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
                } catch {
                    ctx.Connection.Rollback ();
                    throw;
                }
            });
        }

        private class Context : IDataStoreContext
        {
            private readonly SQLiteConnection conn;

            public Context (string dbPath)
            {
                conn = new SQLiteConnection (dbPath);
            }

            public T Put<T> (T obj)
                where T : new()
            {
                conn.InsertOrReplace (obj);

                // TODO: Schedule message to be sent about this update post transaction

                return obj;
            }

            public bool Delete (object obj)
            {
                var count = conn.Delete (obj);

                // TODO: Schedule message to be sent about this update post transaction

                return count > 0;
            }

            public SQLiteConnection Connection {
                get { return conn; }
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
                    if (isWorking)
                        return;
                    isWorking = true;
                }

                try {
                    await Task.Factory.StartNew (ProcessQueue);
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<Logger> ();
                    log.Error (Tag, ex, "Something exploded on the SQLite background thread.");
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
