using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TogglDoodle.Models
{
    public abstract class Model : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static Dictionary<Type, Dictionary<long, WeakReference>> modelCache =
            new Dictionary<Type, Dictionary<long, WeakReference>> ();
        private static Dictionary<Type, long> lastIds =
            new Dictionary<Type, long> ();

        /// <summary>
        /// Gets the shared instance for this model (by id). If there is no shared instance for this the given
        /// model instance is marked as the shared instance. The data from this given model is merged with the
        /// shared model by default (by calling the Merge method).
        /// </summary>
        /// <returns>The shared shared model instance.</returns>
        /// <param name="model">Model for which a shared instance should be returned.</param>
        /// <typeparam name="T">Type of model.</typeparam>
        public static T GetShared<T> (T model)
            where T : Model
        {
            if (typeof(T) != model.GetType ())
                throw new ArgumentException ("Provided model is not of type T.");
            if (model.sharedInstance)
                return model;

            T sharedModel = GetCached<T> (model.Id);
            if (sharedModel == null) {
                MakeShared (model);
                sharedModel = model;
            } else {
                sharedModel.Merge (model);
            }

            return sharedModel;
        }

        private static T GetCached<T> (long id)
            where T : Model
        {
            if (!modelCache.ContainsKey (typeof(T)))
                return null;

            var cache = modelCache [typeof(T)];
            if (!cache.ContainsKey (id))
                return null;

            var inst = cache [id].Target as T;
            if (inst == null) {
                cache.Remove (id);
            }

            return inst;
        }

        private static void MakeShared (Model model)
        {
            model.sharedInstance = true;

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

        public static long NextId<T> ()
            where T : Model
        {
            return NextId (typeof(T));
        }

        private bool sharedInstance;

        private string GetPropertyName<T> (Expression<Func<T>> expr)
        {
            if (expr == null)
                return null;

            var member = expr.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException ("Expression should be in the format of: () => PropertyName", "expr");

            var prop = member.Member as PropertyInfo;
            if (prop == null
                || prop.DeclaringType == null
                || !prop.DeclaringType.IsAssignableFrom (GetType ())
                || prop.GetGetMethod (true).IsStatic)
                throw new ArgumentException ("Expression should be in the format of: () => PropertyName", "expr");

            return prop.Name;
        }

        protected void NotifyPropertyChanging<T> (Expression<Func<T>> expr)
        {
            var propertyChanging = PropertyChanging;
            if (propertyChanging != null)
                propertyChanging (this, new PropertyChangingEventArgs (GetPropertyName (expr)));
        }

        protected void NotifyPropertyChanged<T> (Expression<Func<T>> expr)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
                propertyChanged (this, new PropertyChangedEventArgs (GetPropertyName (expr)));
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
            var propertyChanging = PropertyChanging;
            var propertyChanged = PropertyChanged;

            if (propertyChanging == null && propertyChanged == null) {
                change ();
                return;
            }

            string prop = GetPropertyName (expr);
            if (propertyChanging != null)
                propertyChanging (this, new PropertyChangingEventArgs (GetPropertyName (expr)));
            change ();
            if (propertyChanged != null)
                propertyChanged (this, new PropertyChangedEventArgs (prop));
        }

        protected void MarkDirty ()
        {
            ModifiedAt = DateTime.UtcNow;
            IsDirty = true;
        }

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
                this.DeletedAt = other.DeletedAt;
                this.ModifiedAt = other.ModifiedAt;
                this.IsDirty = other.IsDirty;
            } finally {
                IsMerging = false;
            }
        }

        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        private long id;

        [SQLite.PrimaryKey]
        public long Id {
            get { return id; }
            set {
                if (sharedInstance)
                    throw new InvalidOperationException ("Cannot change Id after being promoted to shared status.");

                if (id == value)
                    return;
                ChangePropertyAndNotify (() => Id, delegate {
                    id = value;
                });
                MarkDirty ();
            }
        }

        private long? remoteId;

        public long? RemoteId {
            get { return remoteId; }
            set {
                if (remoteId == value)
                    return;
                ChangePropertyAndNotify (() => RemoteId, delegate {
                    remoteId = value;
                });
                MarkDirty ();
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
                IsDirty = true;
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
                MarkDirty ();
            }
        }

        private bool dirty;

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

        private bool merging;

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
    }
}
