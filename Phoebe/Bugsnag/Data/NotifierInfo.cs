using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class NotifierInfo
    {
        [JsonProperty ("name")]
        public string Name { get; set; }

        [JsonProperty ("version")]
        public string Version { get; set; }

        [JsonProperty ("url")]
        public string Url { get; set; }
    }
}
