using Newtonsoft.Json;

namespace Toggl.Phoebe._Data.Json
{
    public class OBMJson
    {
        [JsonProperty ("included")]
        public bool Included { get; set; }

        [JsonProperty ("nr")]
        public int Number { get; set; }
    }
}
