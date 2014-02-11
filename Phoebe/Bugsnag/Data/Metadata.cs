using System;
using Newtonsoft.Json.Linq;

namespace Toggl.Phoebe.Bugsnag.Data
{
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
}
