using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
{
    /**
     * TODO: Test for:
     * - correct MarkDirty behaviour
     */
    [JsonObject (MemberSerialization.OptIn)]
    public abstract partial class Model
    {
        private string GetPropertyName<T> (Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (this);
        }

        protected void MarkDirty ()
        {
            if (!IsShared || IsMerging)
                return;
            if (!IsDirty)
                ModifiedAt = DateTime.UtcNow;
            IsDirty = true;
        }

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
