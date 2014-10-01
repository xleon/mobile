using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class ReportJson : CommonJson
    {
        [JsonProperty ("total_grand")]
        public long TotalGrand { get; set; }

        [JsonProperty ("total_billable")]
        public long TotalBillable { get; set; }

        [JsonProperty ("zoom_level")]
        public string ZoomLevel { get; set; }

        [JsonProperty ("activity")]
        public List<ReportRowJson> Activity { get; set; }

        [JsonProperty ("projects")]
        public List<ReportProjectJson> Projects { get; set; }
    }
}
