using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class WorkspaceUserJson : CommonJson
    {
        [JsonProperty ("admin")]
        public bool IsAdmin { get; set; }

        [JsonProperty ("active")]
        public bool IsActive { get; set; }

        [JsonProperty ("name")]
        public string Name { get; set; }

        [JsonProperty ("email")]
        public string Email { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceId { get; set; }

        [JsonProperty ("uid")]
        public long UserId { get; set; }
    }
}
