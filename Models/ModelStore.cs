using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLite;

namespace TogglDoodle.Models
{
    /**
     * What to test for here:
     * - Loading a model, such that it wouldn't be added into the changes queue
     * - Non-persisted shared model exists, new instance is loaded from db and merged into it (persisted or not?)
     * - Creating a new persisted model just by having the IsPersisted set before making shared
     */
    public class ModelStore
    {
        private class DbCommand : SQLiteCommand
        {
            private readonly ModelStore store;

            public DbCommand (ModelStore store, SQLiteConnection conn) : base (conn)
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
            private readonly ModelStore store;

            public DbConnection (ModelStore store, string databasePath) : base (databasePath)
            {
                this.store = store;
            }

            protected override SQLiteCommand NewCommand ()
            {
                return new DbCommand (store, this);
            }
        }

        private readonly SQLiteConnection conn;
        private readonly HashSet<Model> changedModels = new HashSet<Model> ();
        private readonly List<WeakReference> createdModels = new List<WeakReference> ();
        private string propertyIsShared;
        private string propertyIsPersisted;
        private string propertyIsMerging;
        /* What this class should do
         * - Scan for dirty models that need to be saved in the db
         * - Enable lookup of models
         * - Reverse relation lookup
         * - Last ID value restoration
         */
        public ModelStore (string dbPath)
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

        private string GetPropertyName<T> (Model model, Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (model);
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
