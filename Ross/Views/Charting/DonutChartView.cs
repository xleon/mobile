using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Theme;

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

                var delayNoData = (_reportView.Projects.Count == 0) ? 0.6 : 0;
                var delayData = (_reportView.Projects.Count == 0) ? 0 : 0.6;

                if (_reportView.Projects.Count == 0) {
                    noProjectTitleLabel.Text = (_reportView.IsError) ? "DataErrorTitle".Tr () : "NoDataTitle".Tr ();
                    noProjectTextLabel.Text = (_reportView.IsError) ? "DataErrorText".Tr () : "NoDataText".Tr ();
                }

                UIView.Animate (0.4, delayNoData, UIViewAnimationOptions.TransitionNone,
                () => {
                    noProjectTextLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                    noProjectTitleLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                },  () => {});

                UIView.Animate (0.4, delayData, UIViewAnimationOptions.TransitionNone,
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

        private bool _normalSelectionMode;
        public bool NormalSelectionMode
        {
            get {
                return _normalSelectionMode;
            } set {
                if (_normalSelectionMode == value) {
                    return;
                }
                _normalSelectionMode = value;

                for ( var i = 0; i < projectTableView.NumberOfRowsInSection (0); i++) {
                    var cell = (ProjectReportCell)projectTableView.CellAt (NSIndexPath.FromRowSection (i, 0));
                    if (cell != null) {
                        cell.NormalSelectionMode = value;
                        cell.SetSelected (cell.Selected, false);
                    }
                }
            }
        }

        public DonutChartView (RectangleF frame) : base (frame)
        {
        }

        public DonutChartView ()
        {
            TableProjectList = new List<ReportProject> ();
            DonutProjectList = new List<ReportProject> ();

            grayCircle = new UIView ();
            grayCircle.Layer.AddSublayer (new CAShapeLayer () { FillColor = Color.DonutInactiveGray.CGColor});
            Add (grayCircle);

            donutChart = new XYDonutChart () {
                PieRadius = pieRadius,
                DonutLineStroke = lineStroke,
                DataSource = this,
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
                NormalSelectionMode = true;
                var selectedProject = DonutProjectList [e.Index];
                var idx = TableProjectList.FindIndex (p => AreEquals ( p, selectedProject));
                projectTableView.SelectRow (NSIndexPath.FromRowSection (idx, 0), true, UITableViewScrollPosition.Top);
                ((ProjectListSource)projectTableView.Source).LastSelectedIndex = idx;
                totalTimeLabel.Text = selectedProject.FormattedTotalTime;
                totalTimeLabel.Center = new PointF ( donutChart.PieCenter.X, donutChart.PieCenter.Y);
                moneyLabel.Alpha = 0.0f;
            };
            donutChart.DidDeselectSliceAtIndex += (sender, e) => {
                var selectedProject = DonutProjectList [e.Index];
                var idx = TableProjectList.FindIndex (p => AreEquals ( p, selectedProject));
                projectTableView.DeselectRow (NSIndexPath.FromRowSection (idx, 0), true);
            };
            donutChart.DidDeselectAllSlices += (sender, e) => DeselectAllProjects ();

            projectTableView = new UITableView ();
            projectTableView.RegisterClassForCellReuse (typeof (ProjectReportCell), ProjectReportCell.ProjectReportCellId);
            projectTableView.Source = new ProjectListSource (this);
            projectTableView.RowHeight = 40f;
            projectTableView.TableFooterView = new UIView ();
            var insets = projectTableView.ScrollIndicatorInsets;
            insets.Right -= 3.0f;
            projectTableView.ScrollIndicatorInsets = insets;
            Add (projectTableView);

            totalTimeLabel = new UILabel ();
            totalTimeLabel.Apply (Style.ReportsView.DonutTimeLabel);
            Add (totalTimeLabel);

            moneyLabel = new UILabel ();
            moneyLabel.Apply (Style.ReportsView.DonutMoneyLabel);
            Add (moneyLabel);

            noProjectTitleLabel = new UILabel ();
            noProjectTitleLabel.Apply (Style.ReportsView.NoProjectTitle);
            noProjectTitleLabel.Text = "ReportsLoadingTitle".Tr ();
            Add (noProjectTitleLabel);

            noProjectTextLabel = new UILabel ();
            noProjectTextLabel.Apply (Style.ReportsView.DonutMoneyLabel);
            noProjectTextLabel.Lines = 2;
            noProjectTextLabel.Text = "ReportsLoadingText".Tr ();
            Add (noProjectTextLabel);

            topBoder = new UIView ();
            bottomBoder = new UIView ();
            Add (topBoder);
            Add (bottomBoder);

            projectTableView.Alpha = 0;
            moneyLabel.Alpha = 0;
            totalTimeLabel.Alpha = 0;
        }

        private XYDonutChart donutChart;
        private UITableView projectTableView;
        private UILabel totalTimeLabel;
        private UILabel moneyLabel;
        private UILabel noProjectTitleLabel;
        private UILabel noProjectTextLabel;
        private UIView grayCircle;
        private UIView topBoder;
        private UIView bottomBoder;

        const float pieRadius = 80.0f;
        const float lineStroke = 40f;
        const float padding = 24f;
        const float diameter = pieRadius * 2 + lineStroke;

        public List<ReportProject> TableProjectList;
        public List<ReportProject> DonutProjectList;

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            grayCircle.Frame = new RectangleF (0, 0, Bounds.Width, diameter + padding);
            ((CAShapeLayer)grayCircle.Layer.Sublayers [0]).Path = CGPathCreateArc ( grayCircle.Center, pieRadius, 0, Math.PI * 2, lineStroke);
            donutChart.Frame = new RectangleF (0, 0, Bounds.Width, diameter + padding);
            projectTableView.Frame = new RectangleF (0, donutChart.Bounds.Height, Bounds.Width, Bounds.Height - donutChart.Bounds.Height);

            totalTimeLabel.Bounds = new RectangleF ( 0, 0, donutChart.PieRadius * 2 - donutChart.DonutLineStroke, 20);
            totalTimeLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y - 10);
            moneyLabel.Bounds = new RectangleF ( 0, 0, donutChart.PieRadius * 2 - donutChart.DonutLineStroke, 20);
            moneyLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y + 10);

            noProjectTitleLabel.Bounds = new RectangleF ( 0, 0, donutChart.PieRadius * 2, 20);
            noProjectTitleLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y - 20);
            noProjectTextLabel.Bounds = new RectangleF ( 0, 0, donutChart.PieRadius * 2, 35);
            noProjectTextLabel.Center = new PointF (donutChart.PieCenter.X, donutChart.PieCenter.Y + 5 );

            DrawViewBoders (projectTableView, topBoder, bottomBoder);
        }

        public void SelectProjectAt (int index)
        {
            if ( donutChart.UserInteractionEnabled) {
                NormalSelectionMode = true;
                var selectedProject = TableProjectList [index];
                var idx = DonutProjectList.FindIndex (p => AreEquals ( p, selectedProject));
                if (idx != -1) {
                    donutChart.SetSliceSelectedAtIndex (idx);
                } else {
                    donutChart.DeselectAllSlices ();
                }
                totalTimeLabel.Center = new PointF ( donutChart.PieCenter.X, donutChart.PieCenter.Y);
                totalTimeLabel.Text = selectedProject.FormattedTotalTime;
                moneyLabel.Alpha = 0.0f;
            }
        }

        public void DeselectProjectAt (int index)
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

        public void DeselectAllProjects()
        {
            donutChart.DeselectAllSlices ();
            NormalSelectionMode = false;
            for (int i = 0; i < TableProjectList.Count; i++) {
                projectTableView.DeselectRow ( NSIndexPath.FromRowSection ( 0, i), true);
            }
            totalTimeLabel.Text = _reportView.TotalGrand;
            totalTimeLabel.Center = new PointF ( donutChart.PieCenter.X, donutChart.PieCenter.Y - 10);
            moneyLabel.Alpha = 1.0f;
            ((ProjectListSource)projectTableView.Source).LastSelectedIndex = -1;
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            donutChart.Dispose ();
        }

        private CGPath CGPathCreateArc (PointF center, float radius, double startAngle, double endAngle, float lineStroke)
        {
            var path = new CGPath ();
            path.AddArc (center.X, center.Y, radius, Convert.ToSingle (startAngle), Convert.ToSingle (endAngle), false);
            return path.CopyByStrokingPath (lineStroke, CGLineCap.Butt, CGLineJoin.Miter, 10);
        }

        private bool AreEquals ( ReportProject a, ReportProject b)
        {
            return false || a.TotalTime == b.TotalTime && string.Compare (a.Project, b.Project, StringComparison.Ordinal) == 0 && a.Color == b.Color;
        }

        private void DrawViewBoders ( UIView targetView, UIView tp, UIView btm)
        {
            var mask = new CAGradientLayer ();
            mask.Frame = new RectangleF (0, 0, targetView.Bounds.Width, 10);
            mask.Colors = new [] { UIColor.White.CGColor, UIColor.Clear.CGColor };

            tp.Frame = new RectangleF ( targetView.Frame.X, targetView.Frame.Y, targetView.Bounds.Width, 10);
            tp.BackgroundColor = UIColor.White;
            tp.UserInteractionEnabled = false;
            tp.Layer.Mask = mask;

            var maskInverted = new CAGradientLayer ();
            maskInverted.Frame = new RectangleF (0, 0, targetView.Frame.Width, 20);
            maskInverted.Colors = new [] { UIColor.Clear.CGColor, UIColor.White.CGColor};

            btm.Frame = new RectangleF ( targetView.Frame.X, targetView.Frame.Y + targetView.Bounds.Height - 20, targetView.Bounds.Width, 20);
            btm.BackgroundColor = UIColor.White;
            btm.UserInteractionEnabled = false;
            btm.Layer.Mask = maskInverted;
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

            public int LastSelectedIndex
            {
                get;
                set;
            }

            public ProjectListSource (DonutChartView pieChart)
            {
                _owner = pieChart;
                LastSelectedIndex = -1;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (ProjectReportCell)tableView.DequeueReusableCell (ProjectReportCell.ProjectReportCellId);
                cell.Data = _owner.TableProjectList [indexPath.Row];
                cell.NormalSelectionMode = _owner.NormalSelectionMode;
                return cell;
            }

            public override int RowsInSection (UITableView tableview, int section)
            {
                return _owner.TableProjectList.Count;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                if (indexPath.Row != LastSelectedIndex) {
                    _owner.SelectProjectAt (indexPath.Row);
                    LastSelectedIndex = indexPath.Row;
                } else {
                    _owner.DeselectAllProjects ();
                }
            }

            public override void RowDeselected (UITableView tableView, NSIndexPath indexPath)
            {
                _owner.DeselectProjectAt (indexPath.Row);
            }
        }
    }
}