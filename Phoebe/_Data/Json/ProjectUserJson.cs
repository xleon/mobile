using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe._Data.Json
{
    public class ProjectUserJson : CommonJson
    {
        [JsonProperty ("manager")]
        public bool IsManager { get; set; }

        [JsonProperty ("rate")]
        public int HourlyRate { get; set; }

        [JsonProperty ("pid")]
        public long ProjectRemoteId { get; set; }

        [JsonProperty ("uid")]
        public long UserRemoteId { get; set; }
    }
}
