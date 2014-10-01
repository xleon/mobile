using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Reports;

namespace Toggl.Ross.Views.Charting
{
    public class BarChart : UIView, IReportChart
    {
        public EventHandler AnimationStarted { get; set; }

        public EventHandler AnimationEnded { get; set; }

        private SummaryReportView _reportView;

        public SummaryReportView ReportView
        {
            get {
                return _reportView;
            } set {
                _reportView = value;
                drawChartSurface ();
            }
        }

        UIColor[] colors = {
            UIColor.FromRGB (0xBB, 0xBB, 0xBB),
            UIColor.FromRGB (0x03, 0xA9, 0xF3),
            UIColor.FromRGB (0x81, 0xD3, 0xF9),
            UIColor.Red
        };

        readonly UIStringAttributes hoursAttrs = new UIStringAttributes {
            ForegroundColor = UIColor.FromRGB (0xBB, 0xBB, 0xBB),
            BackgroundColor = UIColor.Clear,
            Font = UIFont.FromName ("HelveticaNeue", 9f)
        };

        readonly UIStringAttributes topLabelAttrs = new UIStringAttributes {
            ForegroundColor = UIColor.FromRGB (0x87, 0x87, 0x87),
            BackgroundColor = UIColor.Clear,
            Font = UIFont.FromName ("HelveticaNeue", 12f)
        };

        ChartSurface chartSurface;
        bool drawSurfaceIsReady;

        public BarChart (RectangleF frame) : base (frame)
        {
            chartSurface = new ChartSurface (UIColor.White, colors);
            chartSurface.Frame = frame;
            Add (chartSurface);
            chartSurface.OnReadyToDraw += (sender, e) => {
                drawSurfaceIsReady = true;
                drawChartSurface ();
            };
        }

        void drawChartSurface ()
        {
            if (!drawSurfaceIsReady || _reportView == null) {
                return;
            }

            const float barHeight = 33;
            const float topY = 15;
            const float marginX = 15;
            const float graphHeight = barHeight * 7 + 10;
            const float graphWidth = 190;

            chartSurface.DrawText (new TextDrawingData ("Total", marginX, topY), topLabelAttrs);
            chartSurface.DrawText (new TextDrawingData ("Billable", marginX, topY + 20), topLabelAttrs);
            chartSurface.DrawText (new TextDrawingData (_reportView.TotalGrand, 100, topY), topLabelAttrs);
            chartSurface.DrawText (new TextDrawingData (_reportView.TotalBillale, 118, topY + 20), topLabelAttrs);
            chartSurface.DrawLine (new DoubleDrawingData (marginX + 30, topY + 50, marginX + 30, topY + 63 + graphHeight, 0), 4f);

            for (int i = 1; i < _reportView.ChartTimeLabels ().Count; i++) {
                var x = marginX + 30 + graphWidth / 4 * i;
                chartSurface.DrawLine (new DoubleDrawingData (x, topY + 50, x, topY + 55 + graphHeight, 0), 0.5f);
                chartSurface.DrawText (new TextDrawingData (_reportView.ChartTimeLabels () [i - 1], x - 5, topY + 55 + graphHeight), hoursAttrs);
            }

            for (int i = 0; i < _reportView.Activity.Count; i++) {
                var bar = new SingleBar (chartSurface, "Date", _reportView.Activity [i].BillableTime, _reportView.Activity [i].TotalTime, "€", marginX, topY + 60 + 33 * i);
            }
        }
    }
}

