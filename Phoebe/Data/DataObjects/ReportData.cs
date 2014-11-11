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
            Activity = other.Activity;
            Projects = other.Projects;
        }

        public long TotalGrand { get; set; }

        public long TotalBillable { get; set; }

        public List<ReportActivity> Activity;

        public List<ReportProject> Projects;
    }
}