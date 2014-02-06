using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class StackInfo
    {
        [JsonProperty ("file", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string File { get; set; }

        [JsonProperty ("lineNumber", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Line { get; set; }

        [JsonProperty ("columnNumber", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Column { get; set; }

        [JsonProperty ("method")]
        public string Method { get; set; }

        [JsonProperty ("inProject")]
        public bool InProject { get; set; }
    }
}
