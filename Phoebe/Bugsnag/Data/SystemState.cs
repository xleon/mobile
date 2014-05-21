using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class SystemState
    {
        [JsonProperty ("freeMemory", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ulong FreeMemory { get; set; }

        [JsonProperty ("freeDisk", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ulong AvailableDiskSpace { get; set; }
    }
}
