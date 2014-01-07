using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLite;

namespace Toggl.Phoebe.Models
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
        private string propertyIsShared;
        private string propertyIsPersisted;
        private string propertyIsMerging;

        public SQLiteModelStore (string dbPath)
        {
            conn = new DbConnection (this, dbPath);
            CreateTables (conn);
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

        public T Get<T> (long id)
            where T : Model
        {
            return (T)Get (typeof(T), id);
        }

        public Model Get (Type type, long id)
        {
            if (!type.IsSubclassOf (typeof(Model)))
                throw new ArgumentException ("Type must be of a subclass of Model.", "type");

            var map = conn.GetMapping (type);
            return (Model)conn.Query (map, map.GetByPrimaryKeySql, id).FirstOrDefault ();
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

        private string GetPropertyName<T> (Model model, Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (model);
        }

        public long GetLastId (Type type)
        {
            if (!type.IsSubclassOf (typeof(Model)))
                throw new ArgumentException ("Type must be of a subclass of Model.", "type");

            var map = conn.GetMapping (type);
            var sql = String.Format ("select max({0}) from {1}", map.PK.Name, map.TableName);
            return conn.ExecuteScalar<long> (sql);
        }

        public void ModelChanged (Model model, string property)
        {
            if (!model.IsShared)
                return;

            if (propertyIsShared == null)
                propertyIsShared = GetPropertyName (model, () => model.IsShared);
            if (propertyIsPersisted == null)
                propertyIsPersisted = GetPropertyName (model, () => model.IsPersisted);
            if (propertyIsMerging == null)
                propertyIsMerging = GetPropertyName (model, () => model.IsMerging);

            if (property == propertyIsMerging)
                return;

            if (property == propertyIsShared) {
                // No need to mark newly created property as changed:
                if (createdModels.Any ((r) => r.Target == model))
                    return;
            }

            if (property == propertyIsPersisted || model.IsPersisted) {
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
