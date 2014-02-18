using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe.Data
{
    public partial class Model
    {
        private readonly Dictionary<string, string> propertyErrors = new Dictionary<string, string> ();

        public bool Validate ()
        {
            lock (SyncRoot) {
                ValidateProperties (null);
                return IsValid;
            }
        }

        private void Validate (string propertyName)
        {
            if (propertyName == PropertyErrors || propertyName == PropertyIsValid)
                return;
            ValidateProperties (new string[] { propertyName });
        }

        private void ValidateProperties (string[] properties)
        {
            var ctx = new ValidationContext (properties);
            Validate (ctx);

            if (ctx.CheckForChangedErrors (propertyErrors)) {
                ChangePropertyAndNotify (PropertyErrors, delegate {
                    ctx.MergeErrors (propertyErrors);
                });
            }

            IsValid = propertyErrors.Count == 0;
        }

        protected virtual void Validate (ValidationContext ctx)
        {
        }

        public static readonly string PropertyErrors = GetPropertyName ((m) => m.Errors);

        [DontDirty]
        [SQLite.Ignore]
        public Dictionary<string, string> Errors {
            get {
                lock (SyncRoot) {
                    return propertyErrors;
                }
            }
        }

        private bool valid = true;
        public static readonly string PropertyIsValid = GetPropertyName ((m) => m.IsValid);

        [DontDirty]
        public bool IsValid {
            get {
                lock (SyncRoot) {
                    return valid;
                }
            }
            set {
                lock (SyncRoot) {
                    if (valid == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsValid, delegate {
                        valid = value;
                    });
                }
            }
        }
    }
}