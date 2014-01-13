using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLite;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    /**
     * What to test for here:
     * - Loading a model, such that it wouldn't be added into the changes queue
     * - Non-persisted shared model exists, new instance is loaded from db and merged into it (persisted or not?)
     * - Creating a new persisted model just by having the IsPersisted set before making shared
     */
    public class SQLiteModelStore : IModelStore
    {
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

                var model = obj as Model;
                if (model != null) {
                    model.IsPersisted = true;
                    store.createdModels.Add (new WeakReference (model));
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

        private readonly SQLiteConnection conn;
        private readonly HashSet<Model> changedModels = new HashSet<Model> ();
        private readonly List<WeakReference> createdModels = new List<WeakReference> ();
        private readonly Action<ModelChangedMessage> subscriptionModelChanged;

        public SQLiteModelStore (string dbPath)
        {
            conn = new DbConnection (this, dbPath);
            CreateTables (conn);

            // We have to subscribe to this like so, else mono's GC get's a bit over excited and wipes our
            // subscription before we can receive anything
            subscriptionModelChanged = (msg) => OnModelChangedMessage (msg.Model, msg.PropertyName);
            ServiceContainer.Resolve<Messenger> ().Subscribe<ModelChangedMessage> (subscriptionModelChanged);
        }

        private static void CreateTables (SQLiteConnection db)
        {
            var modelType = typeof(Model);
            // Auto-discover models in single assembly namespace
            var modelSubtypes =
                from t in modelType.Assembly.GetTypes ()
                            where t.Namespace == modelType.Namespace && t.IsSubclassOf (modelType)
                            select t;
            foreach (var t in modelSubtypes) {
                db.CreateTable (t);
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

            var map = conn.GetMapping (type);
            var sql = string.Format (
                          "select * from \"{0}\" where \"{1}\" = ?",
                          map.TableName,
                          map.FindColumnWithPropertyName ("RemoteId")
                      );
            return (Model)conn.Query (map, sql, remoteId).FirstOrDefault ();
        }

        public IModelQuery<T> Query<T> (
            Expression<Func<T, bool>> predExpr = null,
            Func<IEnumerable<T>, IEnumerable<T>> filter = null)
            where T : Model, new()
        {
            IModelQuery<T> query = new DbQuery<T> (conn.Table<T> (), filter);
            if (predExpr != null)
                query = query.Where (predExpr);
            return query;
        }

        private void OnModelChangedMessage (Model model, string property)
        {
            if (!model.IsShared)
                return;

            if (property == Model.PropertyIsMerging)
                return;

            if (property == Model.PropertyIsShared) {
                // No need to mark newly created property as changed:
                if (createdModels.Any ((r) => r.Target == model))
                    return;
            }

            if (property == Model.PropertyIsPersisted || model.IsPersisted) {
                changedModels.Add (model);
            }
        }

        public void Commit ()
        {
            // TODO: Call this from somewhere...
            conn.BeginTransaction ();
            try {
                foreach (var model in changedModels) {
                    if (model.IsPersisted) {
                        if (conn.Update (model) == 0)
                            conn.Insert (model);
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
    }
}
