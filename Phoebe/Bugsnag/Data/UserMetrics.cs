using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class UserMetrics
    {
        [JsonProperty ("apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty ("user")]
        public UserInfo User { get; set; }

        [JsonProperty ("app")]
        public ApplicationInfo App { get; set; }

        [JsonProperty ("device")]
        public SystemInfo System { get; set; }
    }
}
