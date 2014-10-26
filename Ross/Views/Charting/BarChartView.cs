using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Reports;
using System.Collections.Generic;
using Toggl.Phoebe.Data;
using Toggl.Ross.Theme;
using System.Diagnostics;

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
            barChart.ReloadData ();
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

        #region IBarChartDataSource implementation

        public string TimeIntervalAtIndex (int index)
        {
            return _reportView.ChartTimeLabels [index];
        }

        public int NumberOfBarsOnChart (BarChart barChart)
        {
            return ActivityList.Count;
        }

        public float ValueForBarAtIndex (BarChart barChart, int index)
        {
            return (ActivityList [index].TotalTime == 0) ? 0 : (float) (ActivityList [index].TotalTime / TimeSpan.FromHours (_reportView.MaxTotal ).TotalSeconds);
        }

        public float ValueForSecondaryBarAtIndex (BarChart barChart, int index)
        {
            return (ActivityList [index].BillableTime == 0) ? 0 : (float) (ActivityList [index].BillableTime / TimeSpan.FromHours (_reportView.MaxTotal ).TotalSeconds);
        }

        public string TextForBarAtIndex (BarChart barChart, int index)
        {
            return _reportView.ChartRowLabels [index];
        }

        #endregion
    }
}

