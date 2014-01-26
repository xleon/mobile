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
            // Prevent overriding previous error
            string prevError;
            errors.TryGetValue (propertyName, out prevError);
            if (prevError != null)
                return;

            errors [propertyName] = error;
        }

        /// <summary>
        /// Clears any errors on the property with propertyName. Usually if HasChanged returns true for the property,
        /// the errors are cleared automatically. However, for composite validation, this can be used to clear errors
        /// on fields which didn't change.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        public void ClearErrors (string propertyName)
        {
            errors [propertyName] = null;
        }

        public bool CheckForChangedErrors (IDictionary<string, string> existingErrors)
        {
            if (properties == null) {
                // If all of the existing error keys aren't present in changes, something has changed
                foreach (var prop in existingErrors.Keys) {
                    if (!errors.ContainsKey (prop))
                        return true;
                }
            } else {
                // Check for old errors that will be cleared for properties
                foreach (var prop in properties) {
                    if (existingErrors.ContainsKey (prop) && !errors.ContainsKey (prop))
                        return true;
                }
            }

            // Compare error changes with existing errors
            foreach (var kvp in errors) {
                if (kvp.Value == null) {
                    if (existingErrors.ContainsKey (kvp.Key))
                        return true;
                } else {
                    string oldError;
                    if (!existingErrors.TryGetValue (kvp.Key, out oldError))
                        return true;
                    if (oldError != kvp.Value)
                        return true;
                }
            }

            return false;
        }

        public void MergeErrors (Dictionary<string, string> existingErrors)
        {
            if (properties == null) {
                existingErrors.Clear ();
            } else {
                foreach (var propertyName in properties) {
                    existingErrors.Remove (propertyName);
                }
            }

            foreach (var kvp in errors) {
                if (kvp.Value != null) {
                    existingErrors [kvp.Key] = kvp.Value;
                } else {
                    existingErrors.Remove (kvp.Key);
                }
            }
        }
    }
}
