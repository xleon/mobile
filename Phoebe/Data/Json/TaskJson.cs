using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class TaskJson : CommonJson
    {
        [JsonProperty ("name")]
        public string Name { get; set; }

        [JsonProperty ("active")]
        public bool IsActive { get; set; }

        [JsonProperty ("estimated_seconds")]
        public long Estimate { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceId { get; set; }

        [JsonProperty ("pid")]
        public long ProjectId { get; set; }
    }
}
