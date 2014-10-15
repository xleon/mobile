using System;
<<<<<<< HEAD
=======
    using SQLite;
>>>>>>> e09ae4f... working on donut chart
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.DataObjects
{
    <<<<<<< HEAD
    =======
        [Table ("ReportsModel")]
        >>>>>>> e09ae4f... working on donut chart
        public class ReportData : CommonData
    {
        public ReportData ()
        {
        }

        public ReportData (ReportData other) : base (other)
        {
            <<<<<<< HEAD
            TotalGrand = other.TotalGrand;
            TotalBillable = other.TotalBillable;
            =======
                StartDate = other.StartDate;
            TotalGrand = other.TotalGrand;
            TotalBillable = other.TotalBillable;
            ZoomLevel = other.ZoomLevel;
            >>>>>>> e09ae4f... working on donut chart
            Activity = other.Activity;
            Projects = other.Projects;
        }

        <<<<<<< HEAD
        =======
            public DateTime StartDate { get; set; }

            >>>>>>> e09ae4f... working on donut chart
            public long TotalGrand { get; set; }

            public long TotalBillable { get; set; }

            <<<<<<< HEAD
            =======
                public ZoomLevel ZoomLevel { get; set; }

                >>>>>>> e09ae4f... working on donut chart
                public List<ReportActivity> Activity;

        public List<ReportProject> Projects;
    }
    <<<<<<< HEAD
}
=======
}
>>>>>>> e09ae4f... working on donut chart
