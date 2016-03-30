using System.ComponentModel;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class TaskJson : CommonJson
    {
        [DefaultValue ("")]
        [JsonProperty (PropertyName = "name", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Name { get; set; }

        [JsonProperty ("active")]
        public bool IsActive { get; set; }

        [JsonProperty ("estimated_seconds")]
        public long Estimate { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceRemoteId { get; set; }

        [JsonProperty ("pid")]
        public long ProjectRemoteId { get; set; }
    }
}
