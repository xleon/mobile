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
        public static readonly string PropertyIsMerging = GetPropertyName ((m) => m.IsMerging);

        [DontDirty]
        [SQLite.Ignore]
        public bool IsMerging {
            get { return merging; }
            protected set {
                if (merging == value)
                    return;
                ChangePropertyAndNotify (PropertyIsMerging, delegate {
                    merging = value;
                });
            }
        }
    }
}