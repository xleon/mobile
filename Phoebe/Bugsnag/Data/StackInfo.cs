using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class StackInfo
    {
        [JsonProperty ("file")]
        public string File { get; set; }

        [JsonProperty ("lineNumber")]
        public int Line { get; set; }

        [JsonProperty ("columnNumber")]
        public int Column { get; set; }

        [JsonProperty ("method")]
        public string Method { get; set; }

        [JsonProperty ("inProject")]
        public bool InProject { get; set; }
    }
}
