using Newtonsoft.Json;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.Json
{
    public class ReportProjectJson
    {
        [JsonProperty ("project")]
        public string Project { get; set; }

        [JsonProperty ("total_time")]
        public long TotalTime { get; set; }

        [JsonProperty ("client")]
        public string Client { get; set; }

        [JsonProperty ("color")]
        public int? Color { get; set; }

        [JsonProperty ("items")]
        public List<ReportTimeEntryJson> Items { get; set; }
    }

    public class ReportTimeEntryJson
    {
        [JsonProperty ("ids")]
        public List<string> Ids { get; set; }

        [JsonProperty ("time_entry")]
        public string Title { get; set; }

        [JsonProperty ("time")]
        public long Time { get; set; }

        [JsonProperty ("cur")]
        public string Currency { get; set; }

        [JsonProperty ("sum", Required = Required.AllowNull)]
        public float Sum { get; set; }

        [JsonProperty ("rate", Required = Required.AllowNull)]
        public float Rate { get; set; }
    }
}
