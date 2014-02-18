using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Newtonsoft.Json;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    /**
     * TODO: Test for:
     * - correct MarkDirty behaviour
     */
    [JsonObject (MemberSerialization.OptIn)]
    public abstract partial class Model : ObservableObject
    {
        /// <summary>
        /// The sync root to lock on when dealing with models. This is to prevent any other thread from interacting
        /// with the data simultaneously. All public model properties and functions should be wrapped in a lock
        /// statement.
        /// </summary>
        public static readonly object SyncRoot = new object ();
        private static readonly AttributeLookupCache<DontDirtyAttribute> dontDirtyCache =
            new AttributeLookupCache<DontDirtyAttribute> ();

        private static string GetPropertyName<T> (Expression<Func<Model, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        protected override void OnPropertyChanged (string property)
        {
            base.OnPropertyChanged (property);

            if (IsShared) {
                ServiceContainer.Resolve<MessageBus> ().Send (new ModelChangedMessage (this, property));

                if (property == PropertyIsShared) {
                    // Validate whole model when being promoted to shared
                    Validate ();
                } else {
                    Validate (property);
                }

                // Automatically mark the object dirty, if property doesn't explicitly disable it
                if (!dontDirtyCache.HasAttribute (GetType (), property)) {
                    MarkDirty (property != PropertyModifiedAt);
                }
            }
        }

        protected void MarkDirty (bool updateModTime = true)
        {
            if (!IsShared || IsMerging)
                return;

            if (updateModTime)
                ModifiedAt = DateTime.UtcNow;
            IsDirty = true;
        }

        public virtual void Delete ()
        {
            lock (SyncRoot) {
                DeletedAt = DateTime.UtcNow;
                if (RemoteId == null) {
                    IsPersisted = false;
                }
            }
        }

        private Guid? id;
        public static readonly string PropertyId = GetPropertyName ((m) => m.Id);

        [DontDirty]
        [SQLite.PrimaryKey]
        public Guid? Id {
            get {
                lock (SyncRoot) {
                    return id;
                }
            }
            set {
                lock (SyncRoot) {
                    if (IsShared)
                        throw new InvalidOperationException ("Cannot change Id after being promoted to shared status.");

                    if (id == value)
                        return;
                    ChangePropertyAndNotify (PropertyId, delegate {
                        id = value;
                    });
                }
            }
        }

        private long? remoteId;
        public static readonly string PropertyRemoteId = GetPropertyName ((m) => m.RemoteId);

        [DontDirty]
        [JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Unique]
        public long? RemoteId {
            get {
                lock (SyncRoot) {
                    return remoteId;
                }
            }
            set {
                lock (SyncRoot) {
                    if (remoteId == value)
                        return;

                    // Check for constraints
                    if (value != null && IsShared) {
                        if (Model.Manager.GetByRemoteId (GetType (), value.Value) != null) {
                            throw new IntegrityException ("Model with such RemoteId already exists.");
                        }
                    }

                    ChangePropertyAndNotify (PropertyRemoteId, delegate {
                        var oldId = remoteId;
                        remoteId = value;

                        Manager.NotifyRemoteIdChanged (this, oldId, remoteId);
                    });
                }
            }
        }

        private DateTime modified = DateTime.UtcNow;
        public static readonly string PropertyModifiedAt = GetPropertyName ((m) => m.ModifiedAt);

        [JsonProperty ("at")]
        public DateTime ModifiedAt {
            get {
                lock (SyncRoot) {
                    return modified;
                }
            }
            set {
                value = value.ToUtc ();

                lock (SyncRoot) {
                    if (modified == value)
                        return;
                    ChangePropertyAndNotify (PropertyModifiedAt, delegate {
                        modified = value;
                    });
                }
            }
        }

        private DateTime? deleted;
        public static readonly string PropertyDeletedAt = GetPropertyName ((m) => m.DeletedAt);

        public DateTime? DeletedAt {
            get {
                lock (SyncRoot) {
                    return deleted;
                }
            }
            set {
                value = value.ToUtc ();

                lock (SyncRoot) {
                    if (deleted == value)
                        return;
                    ChangePropertyAndNotify (PropertyDeletedAt, delegate {
                        deleted = value;
                    });
                }
            }
        }

        private DateTime? remoteDeleted;
        public static readonly string PropertyRemoteDeletedAt = GetPropertyName ((m) => m.RemoteDeletedAt);

        [JsonProperty ("server_deleted_at", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Ignore]
        public DateTime? RemoteDeletedAt {
            get {
                lock (SyncRoot) {
                    return remoteDeleted;
                }
            }
            set {
                value = value.ToUtc ();

                lock (SyncRoot) {
                    if (remoteDeleted == value)
                        return;

                    ChangePropertyAndNotify (PropertyRemoteDeletedAt, delegate {
                        remoteDeleted = value;
                    });
                }
            }
        }

        private bool dirty;
        public static readonly string PropertyIsDirty = GetPropertyName ((m) => m.IsDirty);

        [DontDirty]
        public bool IsDirty {
            get {
                lock (SyncRoot) {
                    return dirty;
                }
            }
            set {
                lock (SyncRoot) {
                    if (dirty == value)
                        return;
                    ChangePropertyAndNotify (PropertyIsDirty, delegate {
                        dirty = value;
                    });
                }
            }
        }

        private bool persisted;
        public static readonly string PropertyIsPersisted = GetPropertyName ((m) => m.IsPersisted);

        [DontDirty]
        [SQLite.Ignore]
        public bool IsPersisted {
            get {
                lock (SyncRoot) {
                    return persisted;
                }
            }
            set {
                lock (SyncRoot) {
                    if (persisted == value)
                        return;
                    ChangePropertyAndNotify (PropertyIsPersisted, delegate {
                        persisted = value;
                    });
                }
            }
        }

        private volatile bool sharedInstance;
        public static readonly string PropertyIsShared = GetPropertyName ((m) => m.IsShared);

        [DontDirty]
        [SQLite.Ignore]
        public bool IsShared {
            get { return sharedInstance; }
            internal set {
                if (!value || sharedInstance == value)
                    return;

                lock (SyncRoot) {
                    ChangePropertyAndNotify (PropertyIsShared, delegate {
                        sharedInstance = value;
                    });
                }
            }
        }
    }
}
