using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
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

                if (_reportView.Projects.Count == 0) {
                    noProjectTitleLabel.Text = (_reportView.IsError) ? "DataErrorTitle".Tr () : "NoDataTitle".Tr ();
                    noProjectTextLabel.Text = (_reportView.IsError) ? "DataErrorText".Tr () : "NoDataText".Tr ();
                }

                var delayNoData = (_reportView.Projects.Count == 0) ? 0.5 : 0;
                var delayData = (_reportView.Projects.Count == 0) ? 0 : 0.5;

                UIView.Animate (0.4, delayNoData, UIViewAnimationOptions.TransitionNone,
                () => {
                    noProjectTextLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                    noProjectTitleLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                },
                ()=> {
                    totalTimeLabel.Text = _reportView.TotalGrand;
                    moneyLabel.Text = _reportView.TotalBillale;
                    ActivityList = _reportView.Activity;
                    barChart.ReloadData ();
                });

                UIView.Animate (0.5, delayData, UIViewAnimationOptions.TransitionNone,
                () => {
                    barChart.Alpha = (_reportView.Projects.Count == 0) ? 0.5f : 1;
                },  null);
            }
        }

        public BarChartView ()
        {
            ActivityList = new List<ReportActivity> ();

            titleTimeLabel = new UILabel ();
            titleTimeLabel.Text = "ReportsTotalLabel".Tr ();
            titleTimeLabel.Apply (Style.ReportsView.BarCharLabelTitle );
            Add (titleTimeLabel);

            totalTimeLabel = new UILabel ();
            totalTimeLabel.Apply (Style.ReportsView.BarCharLabelValue);
            Add (totalTimeLabel);

            titleMoneyLabel = new UILabel ();
            titleMoneyLabel.Apply (Style.ReportsView.BarCharLabelTitle);
            titleMoneyLabel.Text = "ReportsBillableLabel".Tr ();
            Add (titleMoneyLabel);
            moneyLabel = new UILabel ();
            moneyLabel.Apply (Style.ReportsView.BarCharLabelValue );
            Add (moneyLabel);

            barChart = new BarChart () {
                DataSource = this
            };
            Add (barChart);

            noProjectTitleLabel = new UILabel ();
            noProjectTitleLabel.Center = new PointF (barChart.Center.X, barChart.Center.Y - 20);
            noProjectTitleLabel.Apply (Style.ReportsView.NoProjectTitle);
            noProjectTitleLabel.Text = "ReportsLoadingTitle".Tr ();
            Add (noProjectTitleLabel);

            noProjectTextLabel = new UILabel ();
            noProjectTextLabel.Center = new PointF (barChart.Center.X, barChart.Center.Y + 5 );
            noProjectTextLabel.Apply (Style.ReportsView.DonutMoneyLabel);
            noProjectTextLabel.Lines = 2;
            noProjectTextLabel.Text = "ReportsLoadingText".Tr ();
            Add (noProjectTextLabel);
        }

        public List<ReportActivity> ActivityList;
        BarChart barChart;
        UILabel titleTimeLabel;
        UILabel titleMoneyLabel;
        UILabel totalTimeLabel;
        UILabel moneyLabel;
        UILabel noProjectTitleLabel;
        UILabel noProjectTextLabel;

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            titleTimeLabel.Frame = new RectangleF (0, 0, 120, 20);
            totalTimeLabel.Frame = new RectangleF (Bounds.Width - 120, 0, 120, 20);
            titleMoneyLabel.Frame = new RectangleF (0, 20, 120, 20);
            moneyLabel.Frame = new RectangleF (Bounds.Width - 120, 20, 120, 20);
            barChart.Frame =  new RectangleF (0, 50, Bounds.Width, Bounds.Height - 50);

            noProjectTitleLabel.Bounds = new RectangleF ( 0, 0, Bounds.Width/2, 20);
            noProjectTitleLabel.Center = new PointF (barChart.Center.X, barChart.Center.Y - 20);
            noProjectTextLabel.Bounds = new RectangleF ( 0, 0, Bounds.Width/2, 35);
            noProjectTextLabel.Center = new PointF (barChart.Center.X, barChart.Center.Y + 5 );
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            barChart.Dispose ();
        }

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
            if (_reportView.MaxTotal == 0) {
                return 0;
            }
            return (ActivityList [index].TotalTime == 0) ? 0 : (float) (ActivityList [index].TotalTime / TimeSpan.FromHours (_reportView.MaxTotal ).TotalSeconds);
        }

        public float ValueForSecondaryBarAtIndex (BarChart barChart, int index)
        {
            if (_reportView.MaxTotal == 0) {
                return 0;
            }
            return (ActivityList [index].BillableTime == 0) ? 0 : (float) (ActivityList [index].BillableTime / TimeSpan.FromHours (_reportView.MaxTotal ).TotalSeconds);
        }

        public string TextForBarAtIndex (BarChart barChart, int index)
        {
            return _reportView.ChartRowLabels [index];
        }

        public string TimeForBarAtIndex (int index)
        {
            TimeSpan duration = TimeSpan.FromSeconds ( ActivityList [index].TotalTime);
            return String.Format ("{0:0.00}", duration.TotalHours);
        }

        #endregion
    }
}

