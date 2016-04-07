using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class TagJson : CommonJson
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("wid")]
        public long WorkspaceRemoteId { get; set; }
    }
}
