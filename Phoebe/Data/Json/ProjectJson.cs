using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class ProjectJson : CommonJson
    {
        [JsonProperty ("name")]
        public string Name { get; set; }

        [JsonProperty ("color")]
        public string Color { get; set; }

        [JsonProperty ("active")]
        public bool IsActive { get; set; }

        [JsonProperty ("billable")]
        public bool IsBillable { get; set; }

        [JsonProperty ("is_private")]
        public bool IsPrivate { get; set; }

        [JsonProperty ("template")]
        public bool IsTemplate { get; set; }

        [JsonProperty ("auto_estimates")]
        public bool UseTasksEstimate { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceId { get; set; }

        [JsonProperty ("cid")]
        public long? ClientId { get; set; }
    }
}
