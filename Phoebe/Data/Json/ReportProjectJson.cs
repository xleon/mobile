using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class ReportProjectJson
    {
        [JsonProperty ("project")]
        public string Project { get; set; }

        [JsonProperty ("total_time")]
        public long TotalTime { get; set; }
    }
}
