using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Reports;
using System.Collections.Generic;
using Toggl.Phoebe.Data;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views.Charting
{
    public class BarChartView : UIView, IReportChart, IBarChartDataSource
    {
        public EventHandler GoForwardInterval { get; set; }

        public EventHandler GoBackInterval { get; set; }

        public EventHandler AnimationStarted { get; set; }

        public EventHandler AnimationEnded { get; set; }

        private SummaryReportView _reportView;

        public SummaryReportView ReportView
        {
            get {
                return _reportView;
            } set {
                _reportView = value;

                if (_reportView.Activity == null) {
                    return;
                }

                totalTimeLabel.Text = _reportView.TotalGrand;
                moneyLabel.Text = _reportView.TotalBillale;

                ActivityList = _reportView.Activity;
                barChart.ReloadData ();
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

        public BarChartView (RectangleF frame) : base (frame)
        {
            ActivityList = new List<ReportActivity> ();

            titleTimeLabel = new UILabel (new RectangleF (0, 0, 120, 20));
            titleTimeLabel.Text = "ReportsTotalLabel".Tr ();
            titleTimeLabel.Apply (Style.ReportsView.BarCharLabelTitle );
            Add (titleTimeLabel);
            totalTimeLabel = new UILabel (new RectangleF (frame.Width - 120, 0, 120, 20));
            totalTimeLabel.Apply (Style.ReportsView.BarCharLabelValue);
            Add (totalTimeLabel);

            titleMoneyLabel = new UILabel (new RectangleF (0, 20, 120, 20));
            titleMoneyLabel.Apply (Style.ReportsView.BarCharLabelTitle);
            titleMoneyLabel.Text = "ReportsBillableLabel".Tr ();
            Add (titleMoneyLabel);
            moneyLabel = new UILabel (new RectangleF (frame.Width - 120, 20, 120, 20));
            moneyLabel.Apply (Style.ReportsView.BarCharLabelValue );
            Add (moneyLabel);

            barChart = new BarChart (new RectangleF (0, 50, frame.Width, frame.Height - 50)) {
                DataSource = this
            };
            Add (barChart);
        }

        public List<ReportActivity> ActivityList;
        BarChart barChart;
        UILabel titleTimeLabel;
        UILabel titleMoneyLabel;
        UILabel totalTimeLabel;
        UILabel moneyLabel;
        UIView dragHelperView;

        UIPanGestureRecognizer panGesture;
        UIDynamicAnimator animator;
        UISnapBehavior snap;
        RectangleF snapRect;
        PointF snapPoint;

        void drawChartSurface ()
        {
            const float barHeight = 33;
            const float topY = 15;
            const float marginX = 15;
            const float graphHeight = barHeight * 7 + 10;
            const float graphWidth = 190;

            /*
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
            */
        }

        #region IBarChartDataSource implementation

        public string TimeIntervalAtIndex (int index)
        {
            return _reportView.ChartTimeLabels () [index];
        }

        public int NumberOfBarsOnChart (BarChart barChart)
        {
            return ActivityList.Count;
        }

        public float ValueForBarAtIndex (BarChart barChart, int index)
        {
            return (ActivityList [index].TotalTime == 0) ? 0 : (float) (ActivityList [index].TotalTime / TimeSpan.FromHours (_reportView.GetMaxTotal ()).TotalMilliseconds);
        }

        public string TextForBarAtIndex (BarChart barChart, int index)
        {
            return _reportView.ChartRowLabels() [index];
        }

        #endregion
    }
}

