using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class SystemState
    {
        [JsonProperty ("freeMemory", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long FreeMemory { get; set; }

        [JsonProperty ("freeDisk", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long AvailableDiskSpace { get; set; }
    }
}
