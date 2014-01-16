using System;
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
        private static string GetPropertyName<T> (Expression<Func<Model, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        protected override void OnPropertyChanged (string property)
        {
            base.OnPropertyChanged (property);

            Validate (property);

            ServiceContainer.Resolve<MessageBus> ().Send (new ModelChangedMessage (this, property));

            // Automatically mark the object dirty, if property doesn't explicitly disable it
            var propInfo = GetType ().GetProperty (property);
            if (propInfo.GetCustomAttributes (typeof(DontDirtyAttribute), true).Length == 0) {
                MarkDirty (property != PropertyModifiedAt);
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
            DeletedAt = DateTime.UtcNow;
        }

        private Guid? id;
        public static readonly string PropertyId = GetPropertyName ((m) => m.Id);

        [DontDirty]
        [SQLite.PrimaryKey]
        public Guid? Id {
            get { return id; }
            set {
                if (IsShared)
                    throw new InvalidOperationException ("Cannot change Id after being promoted to shared status.");

                if (id == value)
                    return;
                ChangePropertyAndNotify (PropertyId, delegate {
                    id = value;
                });
            }
        }

        private long? remoteId;
        public static readonly string PropertyRemoteId = GetPropertyName ((m) => m.RemoteId);

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

                ChangePropertyAndNotify (PropertyRemoteId, delegate {
                    var oldId = remoteId;
                    remoteId = value;

                    if (IsShared) {
                        // Update cache index
                        MemoryModelCache cache;
                        if (modelCaches.TryGetValue (GetType (), out cache)) {
                            cache.UpdateRemoteId (this, oldId, remoteId);
                        }
                    }
                });
            }
        }

        private DateTime modified;
        public static readonly string PropertyModifiedAt = GetPropertyName ((m) => m.ModifiedAt);

        [JsonProperty ("at")]
        public DateTime ModifiedAt {
            get { return modified; }
            set {
                value = value.ToUtc ();
                if (modified == value)
                    return;
                ChangePropertyAndNotify (PropertyModifiedAt, delegate {
                    modified = value;
                });
            }
        }

        private DateTime? deleted;
        public static readonly string PropertyDeletedAt = GetPropertyName ((m) => m.DeletedAt);

        public DateTime? DeletedAt {
            get { return deleted; }
            set {
                value = value.ToUtc ();
                if (deleted == value)
                    return;
                ChangePropertyAndNotify (PropertyDeletedAt, delegate {
                    deleted = value;
                });
            }
        }

        private DateTime? remoteDeleted;
        public static readonly string PropertyRemoteDeletedAt = GetPropertyName ((m) => m.RemoteDeletedAt);

        [JsonProperty ("server_deleted_at", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Ignore]
        public DateTime? RemoteDeletedAt {
            get { return remoteDeleted; }
            set {
                value = value.ToUtc ();
                if (remoteDeleted == value)
                    return;

                ChangePropertyAndNotify (PropertyRemoteDeletedAt, delegate {
                    remoteDeleted = value;
                });
            }
        }

        private bool dirty;
        public static readonly string PropertyIsDirty = GetPropertyName ((m) => m.IsDirty);

        [DontDirty]
        public bool IsDirty {
            get { return dirty; }
            set {
                if (dirty == value)
                    return;
                ChangePropertyAndNotify (PropertyIsDirty, delegate {
                    dirty = value;
                });
            }
        }

        private bool persisted;
        public static readonly string PropertyIsPersisted = GetPropertyName ((m) => m.IsPersisted);

        [DontDirty]
        [SQLite.Ignore]
        public bool IsPersisted {
            get { return persisted; }
            set {
                if (persisted == value)
                    return;
                ChangePropertyAndNotify (PropertyIsPersisted, delegate {
                    persisted = value;
                });
            }
        }

        private bool sharedInstance;
        public static readonly string PropertyIsShared = GetPropertyName ((m) => m.IsShared);

        [DontDirty]
        [SQLite.Ignore]
        public bool IsShared {
            get { return sharedInstance; }
            private set {
                if (sharedInstance == value || !value)
                    return;

                ChangePropertyAndNotify (PropertyIsShared, delegate {
                    sharedInstance = value;
                });
            }
        }
    }
}
