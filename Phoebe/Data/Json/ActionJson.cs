using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    [JsonObject (MemberSerialization.OptIn)]
    public class ActionJson
    {
        [JsonProperty ("experiment_id")]
        public int ExperimentId { get; set; }

        [JsonProperty ("key")]
        public string Key { get; set; }

        [JsonProperty ("value")]
        public string Value { get; set; }
    }
}
