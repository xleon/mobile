using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SQLite;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Threading;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class SQLiteModelStore : IModelStore
    {
        private static readonly string LogTag = "SQLiteModelStore";

        private class DbCommand : SQLiteCommand
        {
            private readonly SQLiteModelStore store;

            public DbCommand (SQLiteModelStore store, SQLiteConnection conn) : base (conn)
            {
                this.store = store;
            }

            protected override void OnInstanceCreated (object obj)
            {
                base.OnInstanceCreated (obj);

                lock (Model.SyncRoot) {
                    var model = obj as Model;
                    if (model != null) {
                        model.IsPersisted = true;
                        store.createdModels.Add (new WeakReference (model));
                    }
                }
            }
        }

        private class DbConnection: SQLiteConnection
        {
            private readonly SQLiteModelStore store;

            public DbConnection (SQLiteModelStore store, string databasePath) : base (databasePath)
            {
                this.store = store;
            }

            protected override SQLiteCommand NewCommand ()
            {
                return new DbCommand (store, this);
            }
        }

        private class DbQuery<T> : IModelQuery<T>
            where T : Model, new()
        {
            private readonly TableQuery<T> query;
            private readonly Func<IEnumerable<T>, IEnumerable<T>> filter;

            public DbQuery (TableQuery<T> query, Func<IEnumerable<T>, IEnumerable<T>> filter)
            {
                this.query = query;
                this.filter = filter;
            }

            private DbQuery<T> Wrap (TableQuery<T> query)
            {
                return new DbQuery<T> (query, filter);
            }

            public IModelQuery<T> Where (Expression<Func<T, bool>> predExpr)
            {
                return Wrap (query.Where (predExpr));
            }

            public IModelQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr, bool asc = true)
            {
                if (asc)
                    return Wrap (query.OrderBy (orderExpr));
                else
                    return Wrap (query.OrderByDescending (orderExpr));
            }

            public IModelQuery<T> Take (int n)
            {
                return Wrap (query.Take (n));
            }

            public IModelQuery<T> Skip (int n)
            {
                return Wrap (query.Skip (n));
            }

            public int Count ()
            {
                return query.Count ();
            }

            public IEnumerator<T> GetEnumerator ()
            {
                IEnumerable<T> q = query;
                if (filter != null)
                    q = filter (q);
                return q.GetEnumerator ();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
            {
                return GetEnumerator ();
            }
        }

        private static readonly AttributeLookupCache<IgnoreAttribute> ignoreCache =
            new AttributeLookupCache<IgnoreAttribute> ();
        private readonly ThreadWeakLocal<SQLiteConnection> connection;
        private readonly HashSet<Model> changedModels = new HashSet<Model> ();
        private readonly List<WeakReference> createdModels = new List<WeakReference> ();
        #pragma warning disable 0414
        private readonly Subscription<ModelChangedMessage> modelChangedSubscription;
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
        #pragma warning restore 0414

        public SQLiteModelStore (string dbPath)
        {
            connection = new ThreadWeakLocal<SQLiteConnection> (delegate {
                var conn = new DbConnection (this, dbPath);
                conn.BusyTimeout = TimeSpan.FromSeconds (1);
                return conn;
            });
            CreateTables (connection.Value);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            modelChangedSubscription = bus.Subscribe<ModelChangedMessage> (OnModelChangedMessage, threadSafe: true);
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
        }

        protected virtual void CreateTables (SQLiteConnection db)
        {
            db.BeginTransaction ();
            try {
                foreach (var t in Model.GetAllModels ()) {
                    db.CreateTable (t);
                }
            } finally {
                db.Commit ();
            }
        }

        protected virtual void ClearTables (SQLiteConnection db)
        {
            db.BeginTransaction ();
            try {
                foreach (var t in Model.GetAllModels()) {
                    var map = db.GetMapping (t);
                    var query = string.Format ("delete from \"{0}\"", map.TableName);
                    db.Execute (query);
                }
            } finally {
                db.Commit ();
            }
        }

        public T Get<T> (Guid id)
            where T : Model
        {
            return (T)Get (typeof(T), id);
        }

        public Model Get (Type type, Guid id)
        {
            if (!type.IsSubclassOf (typeof(Model)))
                throw new ArgumentException ("Type must be of a subclass of Model.", "type");

            var conn = connection.Value;
            var map = conn.GetMapping (type);
            return (Model)conn.Query (map, map.GetByPrimaryKeySql, id).FirstOrDefault ();
        }

        public T GetByRemoteId<T> (long remoteId)
            where T : Model
        {
            return (T)GetByRemoteId (typeof(T), remoteId);
        }

        public Model GetByRemoteId (Type type, long remoteId)
        {
            if (!type.IsSubclassOf (typeof(Model)))
                throw new ArgumentException ("Type must be of a subclass of Model.", "type");

            var conn = connection.Value;
            var map = conn.GetMapping (type);
            var sql = string.Format (
                          "select * from \"{0}\" where \"{1}\" = ?",
                          map.TableName,
                          map.FindColumnWithPropertyName ("RemoteId").Name
                      );
            return (Model)conn.Query (map, sql, remoteId).FirstOrDefault ();
        }

        public IModelQuery<T> Query<T> (
            Expression<Func<T, bool>> predExpr = null,
            Func<IEnumerable<T>, IEnumerable<T>> filter = null)
            where T : Model, new()
        {
            var conn = connection.Value;
            IModelQuery<T> query = new DbQuery<T> (conn.Table<T> (), filter);
            if (predExpr != null)
                query = query.Where (predExpr);
            return query;
        }

        private void OnModelChangedMessage (ModelChangedMessage msg)
        {
            lock (Model.SyncRoot) {
                var model = msg.Model;
                var property = msg.PropertyName;

                if (!model.IsShared)
                    return;

                if (property == Model.PropertyIsMerging)
                    return;

                if (property == Model.PropertyIsShared) {
                    // No need to mark newly created property as changed:
                    if (createdModels.Any ((r) => r.Target == model))
                        return;
                }

                // We only care about persisted models (and models which were just marked as non-persistent)
                if (property != Model.PropertyIsPersisted && !model.IsPersisted)
                    return;

                // Ignore changes which we don't store in the database (IsShared && IsPersisted are exceptions)
                if (property != Model.PropertyIsShared
                    && property != Model.PropertyIsPersisted
                    && ignoreCache.HasAttribute (model, property))
                    return;

                changedModels.Add (model);
                ScheduleCommit ();
            }
        }

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            if (msg.AuthManager.IsAuthenticated)
                return;

            lock (Model.SyncRoot) {
                // Wipe database on logout
                ClearTables (connection.Value);
            }
        }

        public void Commit ()
        {
            var conn = connection.Value;
            lock (Model.SyncRoot) {
                conn.BeginTransaction ();
                try {
                    foreach (var model in changedModels) {
                        if (model.IsPersisted) {
                            conn.InsertOrReplace (model);
                        } else {
                            conn.Delete (model);
                        }
                    }
                    changedModels.Clear ();
                    createdModels.Clear ();
                } finally {
                    conn.Commit ();
                }
            }

            ServiceContainer.Resolve<MessageBus> ().Send (
                new ModelsCommittedMessage (this));
        }

        private bool IsScheduled { get; set; }

        protected async virtual void ScheduleCommit ()
        {
            lock (Model.SyncRoot) {
                if (IsScheduled)
                    return;

                IsScheduled = true;
            }

            var reschedule = false;

            try {
                await Task.Delay (TimeSpan.FromMilliseconds (250))
                    .ConfigureAwait (continueOnCapturedContext: false);
                Commit ();
            } catch (SQLiteException ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                if (ex.Result == SQLite3.Result.Busy) {
                    log.Info (LogTag, ex, "Database busy, rescheduling.");
                    reschedule = true;
                } else {
                    log.Warning (LogTag, ex, "Failed to commit changes.");
                    reschedule = true;
                }
            } finally {
                lock (Model.SyncRoot) {
                    IsScheduled = false;
                }
            }

            if (reschedule) {
                ScheduleCommit ();
            }
        }
    }
}
