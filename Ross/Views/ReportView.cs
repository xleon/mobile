using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Views.Charting;
using System.Diagnostics;

namespace Toggl.Ross.Views
{
    public sealed class ReportView : UIScrollView
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

        public bool IsClean
        {
            get;
            set;
        }

        private ChartPosition _position;

        public ChartPosition Position
        {
            get {
                return _position;
            } set {
                _position = value;
                var posY = ( _position == ChartPosition.Top) ? 0 : -pageHeight;
                SetContentOffset (new PointF (_containerView.Center.X, posY), false);
            }
        }

        public ReportView ( RectangleF frame) : base ( frame)
        {
            Frame = new RectangleF (frame.X, frame.Y, frame.Width, frame.Height * 2);

            BackgroundColor = UIColor.White;
            Debug.WriteLine (frame.Height);
            barChart = new BarChartView ( new RectangleF ( padding/2, padding/2, frame.Width - padding, frame.Height - padding - selectorHeight ));
            pieChart = new DonutChartView (new RectangleF (padding/2, barChart.Bounds.Height + padding, frame.Width - padding, frame.Height - selectorHeight));

            ContentSize = new SizeF ( Frame.Width, (frame.Height - padding) * 2 - padding/2);
            pageHeight = barChart.Bounds.Height;

            _containerView = new UIView (frame);
            _containerView.Add (barChart);
            _containerView.Add (pieChart);
            AddSubview (_containerView);

            IsClean = true;
            PagingEnabled = true;
            Bounces = false;
            ShowsVerticalScrollIndicator = false;
        }

        private UIView _containerView;
        private DonutChartView pieChart;
        private BarChartView barChart;
        private SummaryReportView dataSource;
        private bool _loading;
        private float pageHeight = 460;

        const float padding = 24;
        const float navBarHeight = 64;
        const float selectorHeight = 60;

        public async void LoadData()
        {
            if ( IsClean) {
                _loading = true;
                dataSource = new SummaryReportView ();
                dataSource.Period = ZoomLevel;
                await dataSource.Load (TimeSpaceIndex);
                _loading = false;

                if (dataSource.Activity != null) {
                    barChart.ReportView = dataSource;
                    pieChart.ReportView  = dataSource;
                    IsClean = false;
                }
            }
        }

        public void StopReloadData()
        {
            Debug.WriteLine ("Stop Reload Data");
            if (_loading) {
                dataSource.CancelLoad ();
                IsClean = true;
                _loading = false;
            }
        }

        protected override void Dispose (bool disposing)
        {
            barChart.Dispose ();
            pieChart.Dispose ();
            base.Dispose (disposing);
        }

        public enum ChartPosition {
            Top = 0,
            Down = 1
        }

        public override bool GestureRecognizerShouldBegin (UIGestureRecognizer gestureRecognizer)
        {
            return base.GestureRecognizerShouldBegin (gestureRecognizer);
        }
    }
}