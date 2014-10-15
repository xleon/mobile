using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class ReportRowJson
    {
        [JsonProperty ("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty ("total_time")]
        public long TotalTime { get; set; }

        [JsonProperty ("billable_time")]
        public long BillableTime { get; set; }
    }
}