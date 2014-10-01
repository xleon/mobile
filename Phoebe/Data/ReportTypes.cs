using System;

namespace Toggl.Phoebe.Data
{
<<<<<<< HEAD
    public enum ZoomLevel {
=======
<<<<<<< HEAD
    public enum ZoomLevel
    {
>>>>>>> working on donut chart
        Week,
        Month,
        Year
    }

<<<<<<< HEAD
    public struct ReportActivity {
=======
    public struct ReportActivity
    {
=======
    public enum ZoomLevel {
        Day,
        Week,
        Month
    }

    public struct ReportActivity {
>>>>>>> e09ae4f... working on donut chart
>>>>>>> working on donut chart
        public DateTime StartTime { get; set; }

        public long BillableTime { get; set; }

        public long TotalTime { get; set; }
    }

<<<<<<< HEAD
    public struct ReportProject {
=======
<<<<<<< HEAD
    public struct ReportProject
    {
>>>>>>> working on donut chart
        public string Project { get; set; }

        public long TotalTime { get; set; }

        public int Color { get; set; }

    }
}
=======
    public struct ReportProject {
        public string Project { get; set; }

        public long TotalTime { get; set; }
    }
}
>>>>>>> e09ae4f... working on donut chart
