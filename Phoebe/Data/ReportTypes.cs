using System;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data
{
    public enum ZoomLevel {
        Week = 1,
        Month = 2,
        Year = 3
    }

    public struct ReportActivity {

        public DateTime StartTime { get; set; }

        public long BillableTime { get; set; }

        public long TotalTime { get; set; }
    }

    public struct ReportProject {

        public string Project { get; set; }

        public long TotalTime { get; set; }

        public string Client { get; set; }

        public int Color { get; set; }

        public long BillableTime { get; set; }

        public string FormattedTotalTime { get; set; }

        public string FormattedBillableTime { get; set; }

        public List<ReportCurrency> Currencies;

        public List<ReportTimeEntry> Items;
    }

    public struct ReportTimeEntry {

        public string Title { get; set; }

        public long Time { get; set; }

        public string FormattedTime { get; set; }

        public string Currency { get; set; }

        public float Sum { get; set; }

        public float Rate { get; set; }
    }

    public struct ReportCurrency {

        public string Currency { get; set; }

        public float Amount { get; set; }

    }
}