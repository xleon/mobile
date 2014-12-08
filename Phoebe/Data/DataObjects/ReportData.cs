using System.Collections.Generic;

namespace Toggl.Phoebe.Data.DataObjects
{
    public class ReportData
    {
        public ReportData ()
        {
        }

        public ReportData (ReportData other)
        {
            TotalGrand = other.TotalGrand;
            TotalBillable = other.TotalBillable;
            TotalCost = other.TotalCost;
            Activity = other.Activity;
            Projects = other.Projects;
        }

        public string TotalCost;

        public long TotalGrand { get; set; }

        public long TotalBillable { get; set; }

        public List<ReportActivity> Activity;

        public List<ReportProject> Projects;
    }
}