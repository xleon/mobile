using System;
using System.Linq;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data
{
    public class ValidationContext
    {
        private readonly string[] properties;
        private readonly Dictionary<string, string> errors = new Dictionary<string, string> ();

        internal ValidationContext (string[] properties)
        {
            this.properties = properties;
        }

        public bool HasChanged (string propertyName)
        {
            if (properties == null)
                return true;
            return properties.Contains (propertyName);
        }

        public void AddError (string propertyName, string error)
        {
            if (errors.ContainsKey (propertyName))
                return;
            errors [propertyName] = error;
        }

        public void MarkValid (string propertyName)
        {
            errors [propertyName] = null;
        }

        public void GetErrors (Dictionary<string, string> dict)
        {
            if (properties == null) {
                dict.Clear ();
            } else {
                foreach (var propertyName in properties) {
                    dict.Remove (propertyName);
                }
            }

            foreach (var kvp in errors) {
                if (kvp.Value != null) {
                    dict [kvp.Key] = kvp.Value;
                } else {
                    dict.Remove (kvp.Key);
                }
            }
        }
    }
}
