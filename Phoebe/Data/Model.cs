using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;

//#define NotifyPropertyChanging
namespace Toggl.Phoebe.Data
{
    /**
     * TODO: Test for:
     * - correct MarkDirty behaviour
     */
    [JsonObject (MemberSerialization.OptIn)]
    public abstract class Model :
        #if NotifyPropertyChanging
        INotifyPropertyChanging,
        #endif
        INotifyPropertyChanged
    {
        private static Dictionary<Type, MemoryModelCache> modelCaches =
            new Dictionary<Type, MemoryModelCache> ();

        public static IModelStore Store { get; set; }

        /// <summary>
        /// Returns all of the cached shared models.
        /// </summary>
        /// <returns>Enumerable for cached model instances.</returns>
        /// <typeparam name="T">Type of model to get cached instances for.</typeparam>
        public static IEnumerable<T> GetCached<T> ()
            where T : Model
        {
            MemoryModelCache cache;
            if (!modelCaches.TryGetValue (typeof(T), out cache))
                return Enumerable.Empty<T> ();

            return cache.All<T> ();
        }

        public static IEnumerable<Model> GetCached (Type type)
        {
            MemoryModelCache cache;
            if (!modelCaches.TryGetValue (type, out cache))
                return Enumerable.Empty<Model> ();

            return cache.All<Model> ();
        }

        /// <summary>
        /// Gets the shared instance for this model (by Id or RemoteId). If no existing model (in memory and model
        /// store) is found, the given model is promoted to a shared instance status.
        /// When an existing model is found, the data from given instance is merged into the shared instance
        /// automatically.
        /// </summary>
        /// <returns>The shared shared model instance.</returns>
        /// <param name="model">Model for which a shared instance should be returned.</param>
        /// <typeparam name="T">Type of model.</typeparam>
        public static T Update<T> (T model)
            where T : Model
        {
            if (model.IsShared)
                return model;

            T sharedModel = null;

            // First, try to look-up the shared model based on the Id
            if (model.Id.HasValue)
                sharedModel = (T)Get (model.GetType (), model.Id.Value);
            // If that fails, try to use RemoteId
            if (sharedModel == null && model.RemoteId.HasValue)
                sharedModel = (T)GetByRemoteId (model.GetType (), model.RemoteId.Value);

            if (sharedModel != null) {
                sharedModel.Merge (model);
            } else {
                MakeShared (model);
                sharedModel = model;
            }

            return sharedModel;
        }

        /// <summary>
        /// Retrieves the specified model either from cache or from model store.
        /// </summary>
        /// <returns>The shared instance, null if not found.</returns>
        /// <param name="id">Id for the model.</param>
        /// <typeparam name="T">Type of the model.</typeparam>
        public static T Get<T> (Guid id)
            where T : Model
        {
            return (T)Get (typeof(T), id);
        }

        private static Model Get (Type type, Guid id, bool autoLoad = true)
        {
            Model inst = null;
            MemoryModelCache cache;

            // Look through in-memory models:
            if (modelCaches.TryGetValue (type, out cache)) {
                inst = cache.GetById<Model> (id);
            }

            // Try to load from database:
            if (inst == null && autoLoad) {
                inst = Store.Get (type, id);
                if (inst != null) {
                    MakeShared (inst);
                    return inst;
                }
            }

            return inst;
        }

        /// <summary>
        /// Retrieves a shared model by unique RemoteId from cache or from model store.
        /// </summary>
        /// <returns>The shared instance, null if not found.</returns>
        /// <param name="remoteId">Remote identifier.</param>
        /// <typeparam name="T">Type of the model.</typeparam>
        public static T GetByRemoteId<T> (long remoteId)
            where T : Model
        {
            return (T)GetByRemoteId (typeof(T), remoteId);
        }

        private static Model GetByRemoteId (Type type, long remoteId)
        {
            Model inst = null;
            MemoryModelCache cache;

            // Look through in-memory models:
            if (modelCaches.TryGetValue (type, out cache)) {
                inst = cache.GetByRemoteId<Model> (remoteId);
            }

            // Try to load from database:
            if (inst == null) {
                inst = Store.GetByRemoteId (type, remoteId);
                // Check that this model isn't in memory already and having been modified
                if (inst != null && cache != null && cache.GetById<Model> (inst.Id.Value) != null) {
                    inst = null;
                }
                // Mark the loaded model as shared
                if (inst != null) {
                    MakeShared (inst);
                }
            }

            return inst;
        }

        public static IModelQuery<T> Query<T> (Expression<Func<T, bool>> predicate = null)
            where T : Model, new()
        {
            return Store.Query (predicate, (e) => e.Select (UpdateQueryModel));
        }

        private static T UpdateQueryModel<T> (T model)
            where T : Model, new()
        {
            var cached = (T)Get (typeof(T), model.Id.Value, false);
            if (cached != null) {
                cached.Merge (model);
                return cached;
            }

            MakeShared (model);
            return model;
        }

        private static void MakeShared (Model model)
        {
            if (model.Id == null)
                model.Id = Guid.NewGuid ();
            // Enforce integrity
            if (model.RemoteId != null && Model.GetByRemoteId (model.GetType (), model.RemoteId.Value) != null) {
                throw new IntegrityException ("RemoteId is not unique, cannot make shared.");
            }
            model.IsShared = true;

            MemoryModelCache cache;
            var type = model.GetType ();

            if (!modelCaches.TryGetValue (type, out cache)) {
                modelCaches [type] = cache = new MemoryModelCache ();
            }

            cache.Add (model);
        }

        private string GetPropertyName<T> (Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (this);
        }

        #region Property change helpers

        #if NotifyPropertyChanging
        public event PropertyChangingEventHandler PropertyChanging;
        #endif
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanging<T> (Expression<Func<T>> expr)
        {
            #if NotifyPropertyChanging
            OnPropertyChanged (GetPropertyName (expr));
            #endif
        }

        protected virtual void OnPropertyChanging (string property)
        {
            #if NotifyPropertyChanging
            var propertyChanging = PropertyChanging;
            if (propertyChanging != null)
            propertyChanging (this, new PropertyChangingEventArgs (GetPropertyName (expr)));
            #endif
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
            OnPropertyChanging (propertyName);
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

            public Guid? Id { get; set; }

            public string InstanceProperty { get; set; }

            public Type InstanceType { get; set; }

            public Model Instance { get; set; }
        }

        private readonly List<ForeignRelationData> fkRelations = new List<ForeignRelationData> ();

        protected int ForeignRelation<T> (Expression<Func<Guid?>> exprId, Expression<Func<T>> exprInst)
        {
            fkRelations.Add (new ForeignRelationData () {
                IdProperty = GetPropertyName (exprId),
                InstanceProperty = GetPropertyName (exprInst),
                InstanceType = typeof(T),
            });
            return fkRelations.Count;
        }

        protected Guid? GetForeignId (int relationId)
        {
            var fk = fkRelations [relationId - 1];
            return fk.Id;
        }

        protected void SetForeignId (int relationId, Guid? value)
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
            var id = fk.Instance != null ? fk.Instance.Id : (Guid?)null;
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

        public virtual void Merge (Model model)
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

        public virtual void Delete ()
        {
            DeletedAt = DateTime.UtcNow;
        }

        private Guid? id;

        [DontDirty]
        [SQLite.PrimaryKey]
        public Guid? Id {
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
        [JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Unique]
        public long? RemoteId {
            get { return remoteId; }
            set {
                if (remoteId == value)
                    return;

                // Check for constraints
                if (value != null && IsShared) {
                    if (Model.GetByRemoteId (GetType (), value.Value) != null) {
                        throw new IntegrityException ("Model with such RemoteId already exists.");
                    }
                }

                ChangePropertyAndNotify (() => RemoteId, delegate {
                    var oldId = remoteId;
                    remoteId = value;

                    // Update cache index
                    MemoryModelCache cache;
                    if (modelCaches.TryGetValue (GetType (), out cache)) {
                        cache.UpdateRemoteId (this, oldId, remoteId);
                    }
                });
            }
        }

        private DateTime modified;

        [JsonProperty ("at")]
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
