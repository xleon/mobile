using System;
using Toggl.Phoebe.Data.Reports;

namespace Toggl.Ross.Views.Charting
{
    public interface IReportChart
    {
        SummaryReportView ReportView { get; set; }
    }
}