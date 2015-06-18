using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Toggl.Phoebe.Logging
{
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Fatal,
    }

    public class Metadata : JObject
    {
        public Metadata () : base ()
        {
        }

        public Metadata (Metadata other) : base (other)
        {
        }

        public void AddToTab (string tabName, string key, object value)
        {
            var tab = GetTab (tabName);
            if (value != null) {
                tab [key] = JToken.FromObject (value);
            } else {
                tab.Remove (key);
            }
        }

        public void ClearTab (string tabName)
        {
            Remove (tabName);
        }

        private JObject GetTab (string tabName)
        {
            var tab = this [tabName] as JObject;
            if (tab == null) {
                this [tabName] = tab = new JObject ();
            }
            return tab;
        }
    }

    public interface ILoggerClient
    {
        string DeviceId { get; set; }

        List<string> ProjectNamespaces { get; set; }

        void SetUser (string id, string email = null, string name = null);

        void Notify (Exception e, ErrorSeverity severity = ErrorSeverity.Error, Metadata extraMetadata = null);
    }
}

