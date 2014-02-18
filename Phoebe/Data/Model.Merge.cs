using System;
using System.Linq;

namespace Toggl.Phoebe.Data
{
    public partial class Model
    {
        public virtual void Merge (Model model)
        {
            if (model.GetType () != GetType ())
                throw new ArgumentException ("Cannot merge models of different kind", "model");

            lock (SyncRoot) {
                MergeSimpleOverwrite (model);
            }
        }

        protected void MergeSimpleOverwrite (Model other)
        {
            if (this.IsShared && other.IsShared)
                throw new InvalidOperationException ("Cannot merge two shared models.");

            IsMerging = true;
            try {
                // Very simple merging rules: the newest one is always correct, remote deletion overrides everything.
                if (other.ModifiedAt <= this.ModifiedAt && other.RemoteDeletedAt == null)
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
                this.RemoteDeletedAt = other.RemoteDeletedAt;
                if (other.RemoteDeletedAt != null) {
                    // Custom logic for handling remote deletions:
                    this.IsPersisted = false;
                    this.DeletedAt = other.RemoteDeletedAt.Value;
                } else {
                    if (other.IsPersisted)
                        this.IsPersisted = other.IsPersisted;
                    this.DeletedAt = other.DeletedAt;
                }
                this.ModifiedAt = other.ModifiedAt;
                this.IsDirty = other.IsDirty;
            } finally {
                IsMerging = false;
            }
        }

        private bool merging;
        public static readonly string PropertyIsMerging = GetPropertyName ((m) => m.IsMerging);

        [DontDirty]
        [SQLite.Ignore]
        public bool IsMerging {
            get {
                lock (SyncRoot) {
                    return merging;
                }
            }
            protected set {
                lock (SyncRoot) {
                    if (merging == value)
                        return;
                    ChangePropertyAndNotify (PropertyIsMerging, delegate {
                        merging = value;
                    });
                }
            }
        }
    }
}