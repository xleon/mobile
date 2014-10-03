using System;

namespace Toggl.Phoebe.Data
{
    public enum ZoomLevel {
        Week,
        Month,
        Year
    }

    public struct ReportActivity {
        public DateTime StartTime { get; set; }

        public long BillableTime { get; set; }

        public long TotalTime { get; set; }
    }

    public struct ReportProject {
        public string Project { get; set; }

        public long TotalTime { get; set; }

        public int Color { get; set; }

    }
}
