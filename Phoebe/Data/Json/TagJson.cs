using System.ComponentModel;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class TagJson : CommonJson
    {
        [DefaultValue ("")]
        [JsonProperty (PropertyName = "name", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Name { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceId { get; set; }
    }
}
