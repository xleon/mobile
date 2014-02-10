using System;
using Newtonsoft.Json;

namespace Toggl.Joey.Bugsnag.Data
{
    public class ApplicationInfo : Phoebe.Bugsnag.Data.ApplicationInfo
    {
        [JsonProperty ("id")]
        public string Id { get; set; }

        [JsonProperty ("packageName")]
        public string Package { get; set; }

        [JsonProperty ("name")]
        public string Name { get; set; }
    }
}
