using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TogglDoodle.Models
{
    /**
     * TODO: Test for:
     * - correct MarkDirty behaviour
     */
    public abstract class Model : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static Dictionary<Type, Dictionary<long, WeakReference>> modelCache =
            new Dictionary<Type, Dictionary<long, WeakReference>> ();
        private static Dictionary<Type, long> lastIds =
            new Dictionary<Type, long> ();

        public static ModelStore Store { get; set; }

        /// <summary>
        /// Returns all of the cached shared models.
        /// </summary>
        /// <returns>Enumerable for cached model instances.</returns>
        /// <typeparam name="T">Type of model to get cached instances for.</typeparam>
        public static IEnumerable<T> GetCached<T> ()
            where T : Model
        {
            var type = typeof(T);
            if (!modelCache.ContainsKey (type))
                return Enumerable.Empty<T> ();

            return modelCache [type].Values
                    .Select ((r) => r.Target as T)
                    .Where ((m) => m != null);
        }

        public static IEnumerable<Model> GetCached (Type type)
        {
            if (!modelCache.ContainsKey (type))
                return Enumerable.Empty<Model> ();

            return modelCache [type].Values
                    .Select ((r) => r.Target as Model)
                    .Where ((m) => m != null);
        }

        /// <summary>
        /// Gets the shared instance for this model (by id). If there is no shared instance for this the given
        /// model instance is marked as the shared instance. The data from this given model is merged with the
        /// shared model.
        /// </summary>
        /// <returns>The shared shared model instance.</returns>
        /// <param name="model">Model for which a shared instance should be returned.</param>
        /// <typeparam name="T">Type of model.</typeparam>
        public static T Update<T> (T model)
            where T : Model
        {
            if (model.IsShared)
                return model;

            T sharedModel = (T)Get (model.GetType (), model.Id);
            if (sharedModel == null) {
                MakeShared (model);
                sharedModel = model;
            } else {
                sharedModel.Merge (model);
            }

            return sharedModel;
        }

        /// <summary>
        /// Retrieves the specified model either from cache or from model store.
        /// </summary>
        /// <param name="id">Id for the model.</param>
        /// <typeparam name="T">Type of the model.</typeparam>
        public static T Get<T> (long id)
            where T : Model
        {
            return Get (typeof(T), id) as T;
        }

        private static Model Get (Type type, long id)
        {
            Model inst = null;

            // Look through in-memory models:
            if (modelCache.ContainsKey (type)) {
                var cache = modelCache [type];
                if (cache.ContainsKey (id)) {
                    inst = cache [id].Target as Model;
                    if (inst == null) {
                        cache.Remove (id);
                    } else {
                        return inst;
                    }
                }
            }

            // Try to load from database:
            inst = Store.Get (type, id);
            if (inst != null) {
                MakeShared (inst);
                return inst;
            }

            return inst;
        }

        private static void MakeShared (Model model)
        {
            model.IsShared = true;

            var type = model.GetType ();
            if (!modelCache.ContainsKey (type)) {
                modelCache [type] = new Dictionary<long, WeakReference> ();
            }

            modelCache [type] [model.Id] = new WeakReference (model);
        }

        private static long NextId (Type modelType)
        {
            // TODO: Add persisting of last ids, etc.
            if (!lastIds.ContainsKey (modelType)) {
                lastIds [modelType] = 1;
                return 1;
            } else {
                var id = lastIds [modelType] + 1;
                lastIds [modelType] = id;
                return id;
            }
        }

        protected static long NextId<T> ()
            where T : Model
        {
            return NextId (typeof(T));
        }

        private string GetPropertyName<T> (Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (this);
        }

        #region Property change helpers

        protected void OnPropertyChanging<T> (Expression<Func<T>> expr)
        {
            var propertyChanging = PropertyChanging;
            if (propertyChanging != null)
                propertyChanging (this, new PropertyChangingEventArgs (GetPropertyName (expr)));
        }

        /// <summary>
        /// Helper function to call PropertyChanging and PropertyChanged events before and after
        /// the property has been changed.
        /// </summary>
        /// <param name="expr">Expression in the format of () =&gt; PropertyName for the name of the argument of the events.</param>
        /// <param name="change">Delegate to do the actual property changing.</param>
        /// <typeparam name="T">Type of the property being changed (compiler will deduce this for you).</typeparam>
        protected void ChangePropertyAndNotify<T> (Expression<Func<T>> expr, Action change)
        {
            ChangePropertyAndNotify (GetPropertyName (expr), change);
        }

        protected void ChangePropertyAndNotify (string propertyName, Action change)
        {
            var propertyChanging = PropertyChanging;

            if (propertyChanging != null)
                propertyChanging (this, new PropertyChangingEventArgs (propertyName));
            change ();
            OnPropertyChanged (propertyName);
        }

        protected void OnPropertyChanged<T> (Expression<Func<T>> expr)
        {
            OnPropertyChanged (GetPropertyName (expr));
        }

        protected virtual void OnPropertyChanged (string property)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
                propertyChanged (this, new PropertyChangedEventArgs (property));

            if (Store != null)
                Store.ModelChanged (this, property);

            // Automatically mark the object dirty, if property doesn't explicitly disable it
            var propInfo = GetType ().GetProperty (property);
            if (propInfo.GetCustomAttributes (typeof(DontDirtyAttribute), true).Length == 0) {
                MarkDirty ();
            }
        }

        #endregion

        #region Foreign relations helpers

        private class ForeignRelationData
        {
            public string IdProperty { get; set; }

            public long? Id { get; set; }

            public string InstanceProperty { get; set; }

            public Type InstanceType { get; set; }

            public Model Instance { get; set; }
        }

        private readonly List<ForeignRelationData> fkRelations = new List<ForeignRelationData> ();

        protected int ForeignRelation<T> (Expression<Func<long?>> exprId, Expression<Func<T>> exprInst)
        {
            fkRelations.Add (new ForeignRelationData () {
                IdProperty = GetPropertyName (exprId),
                InstanceProperty = GetPropertyName (exprInst),
                InstanceType = typeof(T),
            });
            return fkRelations.Count;
        }

        protected long? GetForeignId (int relationId)
        {
            var fk = fkRelations [relationId - 1];
            return fk.Id;
        }

        protected void SetForeignId (int relationId, long? value)
        {
            var fk = fkRelations [relationId - 1];
            if (fk.Id == value)
                return;

            ChangePropertyAndNotify (fk.IdProperty, delegate {
                fk.Id = value;
            });

            // Try to resolve id to model
            Model inst = null;
            if (fk.Id != null) {
                inst = Model.GetCached (fk.InstanceType).FirstOrDefault ((m) => m.Id == fk.Id.Value);
            }
            if (inst != fk.Instance) {
                ChangePropertyAndNotify (fk.InstanceProperty, delegate {
                    fk.Instance = inst;
                });
            }
        }

        protected T GetForeignModel<T> (int relationId)
            where T : Model
        {
            var fk = fkRelations [relationId - 1];
            if (fk.Instance != null)
                return (T)fk.Instance;

            if (fk.Id != null) {
                // Lazy loading, try to load the value from shared models, or database.
                var inst = Model.Get (fk.InstanceType, fk.Id.Value);

                ChangePropertyAndNotify (fk.InstanceProperty, delegate {
                    fk.Instance = inst;
                });
            }

            return (T)fk.Instance;
        }

        protected void SetForeignModel<T> (int relationId, T value)
            where T : Model
        {
            var fk = fkRelations [relationId - 1];
            if (value != null)
                value = Model.Update (value);
            if (fk.Instance == value)
                return;

            ChangePropertyAndNotify (fk.InstanceProperty, delegate {
                fk.Instance = value;
            });

            // Update current id:
            var id = fk.Instance != null ? fk.Instance.Id : (long?)null;
            if (fk.Id != id) {
                ChangePropertyAndNotify (fk.IdProperty, delegate {
                    fk.Id = id;
                });
            }
        }

        #endregion

        protected void MarkDirty ()
        {
            if (!IsShared || IsMerging)
                return;
            if (!IsDirty)
                ModifiedAt = DateTime.UtcNow;
            IsDirty = true;
        }

        #region Merge functionality

        protected virtual void Merge (Model model)
        {
            if (model.GetType () != GetType ())
                throw new ArgumentException ("Cannot merge models of different kind", "model");

            MergeSimpleOverwrite (model);
        }

        protected void MergeSimpleOverwrite (Model other)
        {
            IsMerging = true;
            try {
                // Very simple merging rules: the newest one is always correct.
                if (other.ModifiedAt <= this.ModifiedAt)
                    return;

                // Update properties defined in subclasses:
                var props =
                    from p in GetType ().GetProperties ()
                                   where p.CanRead && p.CanWrite && p.DeclaringType != typeof(Model)
                                   select p;

                foreach (var prop in props) {
                    var val = prop.GetValue (other, null);
                    prop.SetValue (this, val, null);
                }

                // Update our own properties in a specific order:
                this.RemoteId = other.RemoteId;
                if (other.IsPersisted)
                    this.IsPersisted = other.IsPersisted;
                this.DeletedAt = other.DeletedAt;
                this.ModifiedAt = other.ModifiedAt;
                this.IsDirty = other.IsDirty;
            } finally {
                IsMerging = false;
            }
        }

        private bool merging;

        [DontDirty]
        [SQLite.Ignore]
        public bool IsMerging {
            get { return merging; }
            protected set {
                if (merging == value)
                    return;
                ChangePropertyAndNotify (() => IsMerging, delegate {
                    merging = value;
                });
            }
        }

        #endregion

        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void Delete ()
        {
            DeletedAt = DateTime.UtcNow;
        }

        private long id;

        [DontDirty]
        [SQLite.PrimaryKey]
        public long Id {
            get { return id; }
            set {
                if (IsShared)
                    throw new InvalidOperationException ("Cannot change Id after being promoted to shared status.");

                if (id == value)
                    return;
                ChangePropertyAndNotify (() => Id, delegate {
                    id = value;
                });
            }
        }

        private long? remoteId;

        [DontDirty]
        public long? RemoteId {
            get { return remoteId; }
            set {
                if (remoteId == value)
                    return;
                ChangePropertyAndNotify (() => RemoteId, delegate {
                    remoteId = value;
                });
            }
        }

        private DateTime modified;

        public DateTime ModifiedAt {
            get { return modified; }
            set {
                if (modified == value)
                    return;
                ChangePropertyAndNotify (() => RemoteId, delegate {
                    modified = value;
                });
            }
        }

        private DateTime? deleted;

        public DateTime? DeletedAt {
            get { return deleted; }
            set {
                if (deleted == value)
                    return;
                ChangePropertyAndNotify (() => DeletedAt, delegate {
                    deleted = value;
                });
            }
        }

        private bool dirty;

        [DontDirty]
        public bool IsDirty {
            get { return dirty; }
            set {
                if (dirty == value)
                    return;
                ChangePropertyAndNotify (() => IsDirty, delegate {
                    dirty = value;
                });
            }
        }

        private bool persisted;

        [DontDirty]
        [SQLite.Ignore]
        public bool IsPersisted {
            get { return persisted; }
            set {
                if (persisted == value)
                    return;
                ChangePropertyAndNotify (() => IsPersisted, delegate {
                    persisted = value;
                });
            }
        }

        private bool sharedInstance;

        [DontDirty]
        [SQLite.Ignore]
        public bool IsShared {
            get { return sharedInstance; }
            private set {
                if (sharedInstance == value || !value)
                    return;

                ChangePropertyAndNotify (() => IsShared, delegate {
                    sharedInstance = value;
                });
            }
        }
    }
}
