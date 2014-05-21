using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class SystemInfo
    {
        [JsonProperty ("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty ("osName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string OperatingSystem { get; set; }

        [JsonProperty ("osVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string OperatingSystemVersion { get; set; }

        [JsonProperty ("totalMemory", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ulong TotalMemory { get; set; }

        [JsonProperty ("locale", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Locale { get; set; }
    }
}
