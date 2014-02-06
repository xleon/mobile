using System;
using Newtonsoft.Json;

namespace Toggl.Joey.Bugsnag.Data
{
    public class SystemInfo : Phoebe.Bugsnag.Data.SystemInfo
    {
        [JsonProperty ("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty ("model")]
        public string Model { get; set; }

        [JsonProperty ("screenDensity")]
        public float ScreenDensity { get; set; }

        [JsonProperty ("apiLevel")]
        public int ApiLevel { get; set; }

        [JsonProperty ("jailbroken")]
        public bool IsRooted { get; set; }
    }
}
