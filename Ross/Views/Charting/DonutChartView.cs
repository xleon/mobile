using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Theme;
using System.Linq;
using System.Diagnostics;

namespace Toggl.Ross.Views.Charting
{
    public class DonutChartView : UIView, IReportChart, IXYDonutChartDataSource
    {
        public EventHandler AnimationEnded { get; set; }

        public EventHandler AnimationStarted { get; set; }

        public EventHandler GoForwardInterval { get; set; }

        public EventHandler GoBackInterval { get; set; }

        public EventHandler ChangeView { get; set; }

        private SummaryReportView _reportView;

        public SummaryReportView ReportView
        {
            get {
                return _reportView;
            } set {
                _reportView = value;

                if (_reportView.Projects == null) {
                    return;
                }

                var delayNoData = (_reportView.Projects.Count == 0) ? 0.5 : 0;
                var delayData = (_reportView.Projects.Count == 0) ? 0 : 0.5;

                UIView.Animate (0.5, delayNoData, UIViewAnimationOptions.TransitionNone,
                () => {
                    noProjectTextLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                    noProjectTitleLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                },  () => {});

                UIView.Animate (0.5, delayData, UIViewAnimationOptions.TransitionNone,
                () => {
                    moneyLabel.Alpha = (_reportView.Projects.Count == 0) ? 0 : 1;;
                    projectTableView.Alpha = (_reportView.Projects.Count == 0) ? 0 : 1;
                    totalTimeLabel.Alpha = (_reportView.Projects.Count == 0) ? 0 : 1;
                },  () => {
                    grayCircle.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                });

                _reportView.Projects.Sort ((x, y) => y.TotalTime.CompareTo ( x.TotalTime));
                if (_reportView.Projects.Count == 0) {
                    grayCircle.Alpha = 1;
                }

                const float maxAngle = 3.0f / 360f; // angle in degrees
                var totalValue = Convert.ToSingle ( _reportView.Projects.Sum (p => p.TotalTime));
                DonutProjectList = _reportView.Projects.Where (p => Convert.ToSingle ( p.TotalTime) / totalValue > maxAngle).ToList ();
                TableProjectList = new List<ReportProject> (_reportView.Projects);

                donutChart.UserInteractionEnabled = (DonutProjectList.Count > 1);
                donutChart.ReloadData ();
                projectTableView.ReloadData ();

                totalTimeLabel.Text = _reportView.TotalGrand;
                moneyLabel.Text = _reportView.TotalBillale;
            }
        }

        public List<ReportProject> TableProjectList;
        public List<ReportProject> DonutProjectList;

        public DonutChartView (RectangleF frame) : base (frame)
        {
            const float pieRadius = 80.0f;
            const float lineStroke = 40f;
            const float padding = 24f;
            const float diameter = pieRadius * 2 + lineStroke;

            TableProjectList = new List<ReportProject> ();
            DonutProjectList = new List<ReportProject> ();

            grayCircle = new UIView (new RectangleF (0, 0, frame.Width, diameter + padding));
            grayCircle.Layer.AddSublayer ( CGPathCreateArc ( grayCircle.Center, pieRadius, 0, Math.PI * 2, lineStroke));
            Add (grayCircle);

            donutChart = new XYDonutChart (new RectangleF (0, 0, frame.Width, diameter + padding)) {
                DataSource = this,
                PieRadius = pieRadius,
                DonutLineStroke = lineStroke,
                UserInteractionEnabled = true,
                SelectedSliceStroke = 0,
                ShowPercentage = false,
                StartPieAngle = Math.PI * 3/2,
                ShowLabel = false,
                AnimationSpeed = 1.0f,
                SelectedSliceOffsetRadius = 8f
            };
            Add (donutChart);

            donutChart.DidSelectSliceAtIndex += (sender, e) => {
                var selectedProject = DonutProjectList [e.Index];
                var idx = TableProjectList.FindIndex (p => areEquals ( p, selectedProject));
                projectTableView.SelectRow (NSIndexPath.FromRowSection (idx, 0), true, UITableViewScrollPosition.Top);
            };
            donutChart.DidDeselectSliceAtIndex += (sender, e) => {
                var selectedProject = DonutProjectList [e.Index];
                var idx = TableProjectList.FindIndex (p => areEquals ( p, selectedProject));
                projectTableView.DeselectRow (NSIndexPath.FromRowSection (idx, 0), true);
            };
            donutChart.DidDeselectAllSlices += (sender, e) => {
                for (int i = 0; i < TableProjectList.Count; i++) {
                    projectTableView.DeselectRow ( NSIndexPath.FromRowSection ( 0, i), true);
                }
            };

            projectTableView = new UITableView (new RectangleF (0, donutChart.Frame.Height, frame.Width, frame.Height - donutChart.Frame.Height));
            projectTableView.RegisterClassForCellReuse (typeof (ProjectReportCell), ProjectReportCell.ProjectReportCellId);
            projectTableView.Source = new ProjectListSource (this);
            projectTableView.RowHeight = lineStroke;
            Add (projectTableView);

            totalTimeLabel = new UILabel (new RectangleF ( 0, 0, donutChart.PieRadius * 2 - donutChart.DonutLineStroke, 20));
            totalTimeLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y - 10);
            totalTimeLabel.Apply (Style.ReportsView.DonutTimeLabel);
            Add (totalTimeLabel);

            moneyLabel = new UILabel (new RectangleF ( 0, 0, donutChart.PieRadius * 2 - donutChart.DonutLineStroke, 20));
            moneyLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y + 10);
            moneyLabel.Apply (Style.ReportsView.DonutMoneyLabel);
            Add (moneyLabel);

            noProjectTitleLabel = new UILabel ( new RectangleF ( 0, 0, donutChart.PieRadius * 2, 20));
            noProjectTitleLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y - 20);
            noProjectTitleLabel.Apply (Style.ReportsView.NoProjectTitle);
            noProjectTitleLabel.Text = "NoDataTitle".Tr ();
            Add (noProjectTitleLabel);

            noProjectTextLabel = new UILabel ( new RectangleF ( 0, 0, donutChart.PieRadius * 2, 35));
            noProjectTextLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y + 5 );
            noProjectTextLabel.Apply (Style.ReportsView.DonutMoneyLabel);
            noProjectTextLabel.Lines = 2;
            noProjectTextLabel.Text = "NoDataText".Tr ();
            Add (noProjectTextLabel);

            totalTimeLabel.Alpha = 0;
            moneyLabel.Alpha = 0;
            projectTableView.Alpha = 0;
        }

        XYDonutChart donutChart;
        UITableView projectTableView;
        UILabel totalTimeLabel;
        UILabel moneyLabel;
        UILabel noProjectTitleLabel;
        UILabel noProjectTextLabel;
        UIView grayCircle;

        public void SetSelectedProject (int index)
        {
            if ( donutChart.UserInteractionEnabled) {
                var selectedProject = TableProjectList [index];
                var idx = DonutProjectList.FindIndex (p => areEquals ( p, selectedProject));
                if (idx != -1) {
                    donutChart.SetSliceSelectedAtIndex (idx);
                } else {
                    donutChart.DeselectAllSlices ();
                }
            }
        }

        public void SetDeselectedProject (int index)
        {
            if ( donutChart.UserInteractionEnabled) {
                var selectedProject = TableProjectList [index];
                var idx = DonutProjectList.FindIndex (p => string.Compare (p.Project, selectedProject.Project, StringComparison.Ordinal) == 0 &&
                                                      p.TotalTime == selectedProject.TotalTime);
                if ( idx != -1) {
                    donutChart.SetSliceDeselectedAtIndex (idx);
                }
            }
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            donutChart.Dispose ();
        }

        private CAShapeLayer CGPathCreateArc (PointF center, float radius, double startAngle, double endAngle, float lineStroke)
        {
            var shapeLayer = new CAShapeLayer ();
            var path = new CGPath ();
            path.AddArc (center.X, center.Y, radius, Convert.ToSingle (startAngle), Convert.ToSingle (endAngle), false);
            shapeLayer.Path = path.CopyByStrokingPath (lineStroke, CGLineCap.Butt, CGLineJoin.Miter, 10);
            shapeLayer.FillColor = Color.DonutInactiveGray.CGColor;
            return shapeLayer;
        }

        private bool areEquals ( ReportProject a, ReportProject b)
        {
            return false || a.TotalTime == b.TotalTime && string.Compare (a.Project, b.Project, StringComparison.Ordinal) == 0 && a.Color == b.Color;
        }

        #region Pie Datasource

        public int NumberOfSlicesInPieChart (XYDonutChart pieChart)
        {
            return DonutProjectList.Count;
        }

        public float ValueForSliceAtIndex (XYDonutChart pieChart, int index)
        {
            return DonutProjectList [index].TotalTime;
        }

        public UIColor ColorForSliceAtIndex (XYDonutChart pieChart, int index)
        {
            var hex = ProjectModel.HexColors [DonutProjectList [index].Color % ProjectModel.HexColors.Length];
            return UIColor.Clear.FromHex (hex);
        }

        public string TextForSliceAtIndex (XYDonutChart pieChart, int index)
        {
            return String.Empty;
        }

        #endregion

        internal class ProjectListSource : UITableViewSource
        {
            private readonly DonutChartView _owner;

            public ProjectListSource (DonutChartView pieChart)
            {
                _owner = pieChart;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (ProjectReportCell)tableView.DequeueReusableCell (ProjectReportCell.ProjectReportCellId);
                cell.Data = _owner.TableProjectList [indexPath.Row];
                return cell;
            }

            public override int RowsInSection (UITableView tableview, int section)
            {
                return _owner.TableProjectList.Count;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                _owner.SetSelectedProject (indexPath.Row);
            }

            public override void RowDeselected (UITableView tableView, NSIndexPath indexPath)
            {
                _owner.SetDeselectedProject (indexPath.Row);
            }
        }
    }
}