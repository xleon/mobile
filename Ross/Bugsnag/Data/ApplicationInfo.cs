using System;
using Newtonsoft.Json;

namespace Toggl.Ross.Bugsnag.Data
{
    public class ApplicationInfo : Phoebe.Bugsnag.Data.ApplicationInfo
    {
        [JsonProperty ("id")]
        public string Id { get; set; }

        [JsonProperty ("bundleVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string BundleVersion { get; set; }

        [JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}
