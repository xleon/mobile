using System;
using Newtonsoft.Json;

namespace Toggl.Ross.Bugsnag.Data
{
    public class SystemInfo : Phoebe.Bugsnag.Data.SystemInfo
    {
        [JsonProperty ("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty ("model")]
        public string Model { get; set; }

        [JsonProperty ("diskSize", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ulong DiskSize { get; set; }

        [JsonProperty ("screenDensity")]
        public float ScreenDensity { get; set; }

        [JsonProperty ("screenResolution")]
        public string ScreenResolution { get; set; }

        [JsonProperty ("jailbroken")]
        public bool IsJailbroken { get; set; }
    }
}
