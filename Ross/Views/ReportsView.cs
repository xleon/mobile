using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Views.Charting;

namespace Toggl.Ross.Views
{
    public sealed class ReportsView : UIView
    {
        public ZoomLevel ZoomLevel
        {
            get;
            set;
        }

        public int TimeSpaceIndex
        {
            get;
            set;
        }

        public float BarChartHeight
        {
            get { return barChart.Frame.Height + padding; }
        }

        public ReportsView ( RectangleF frame)
        {
            Frame = new RectangleF (frame.X, frame.Y, frame.Width, frame.Height * 2);
            dataSource = new SummaryReportView ();
            BackgroundColor = UIColor.White;
            barChart = new BarChartView ( new RectangleF ( 0.0f, 0.0f, frame.Width, frame.Height - 2 * selectorHeight));
            pieChart = new DonutChartView (new RectangleF ( 0.0f, barChart.Bounds.Height + padding, frame.Width, frame.Height));
            Add (barChart);
            Add (pieChart);
            _clean = true;
        }

        private DonutChartView pieChart;
        private BarChartView barChart;
        private SummaryReportView dataSource;
        private bool _clean;

        const float padding  = 24;
        const float navBarHeight = 64;
        const float selectorHeight = 50;

        public async void ReloadData()
        {
            if ( _clean || dataSource.Period != ZoomLevel) {
                _clean = false;
                dataSource.Period = ZoomLevel;
                await dataSource.Load (TimeSpaceIndex);
                if ( dataSource.Activity == null) {
                    return;
                }
                pieChart.ReportView = barChart.ReportView = dataSource;
                //pieChart.ReportView = dataSource;
            }
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            barChart.Dispose ();
            pieChart.Dispose ();
        }
    }
}