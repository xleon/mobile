using Toggl.Phoebe.Reports;

namespace Toggl.Ross.Views.Charting
{
    public interface IReportChart
    {
        SummaryReportView ReportView { get; set; }
    }
}