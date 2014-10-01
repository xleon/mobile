using System;
using Toggl.Phoebe.Data.Reports;

namespace Toggl.Ross.Views.Charting
{
    public interface IReportChart
    {
        EventHandler AnimationEnded { get; set; }

        EventHandler AnimationStarted { get; set; }

        SummaryReportView ReportView { get; set; }
    }
}