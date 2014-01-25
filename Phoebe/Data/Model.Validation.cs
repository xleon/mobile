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
            ValidateProperties (null);
            return IsValid;
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

            if (ctx.HasErrors) {
                ChangePropertyAndNotify (PropertyErrors, delegate {
                    ctx.GetErrors (propertyErrors);
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
            get { return propertyErrors; }
        }

        private bool valid = true;
        public static readonly string PropertyIsValid = GetPropertyName ((m) => m.IsValid);

        [DontDirty]
        public bool IsValid {
            get { return valid; }
            set {
                if (valid == value)
                    return;

                ChangePropertyAndNotify (PropertyIsValid, delegate {
                    valid = value;
                });
            }
        }
    }
}