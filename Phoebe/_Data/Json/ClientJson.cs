using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Toggl.Phoebe._Data.Json
{
    public class ClientJson : CommonJson
    {
        [DefaultValue ("")]
        [JsonProperty (PropertyName = "name", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Name { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceRemoteId { get; set; }
    }
}
