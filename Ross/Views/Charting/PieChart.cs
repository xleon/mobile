using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views.Charting
{
    public class PieChart : UIView, IReportChart, IXYDonutChartDataSource
    {
        public EventHandler AnimationEnded { get; set; }

        public EventHandler AnimationStarted { get; set; }

        private SummaryReportView _reportView;

        public SummaryReportView ReportView
        {
            get {
                return _reportView;
            } set {
                _reportView = value;

                var newList = new List<ReportProject> (_reportView.Projects);
                _reportView.Projects.Sort ((x, y) => string.Compare (x.Project, y.Project, StringComparison.Ordinal));

                if (_reportView.Projects.Count == 0) {

                    ProjectList.Clear ();
                    donutChart.ReloadData ();

                } else if (_reportView.Projects.Count >= ProjectList.Count) {

                    for (int i = 0; i < ProjectList.Count; i++) {
                        ProjectList [i] = _reportView.Projects [i];
                    }
                    donutChart.ReloadData ();

                    for (int i = ProjectList.Count; i < _reportView.Projects.Count; i++) {
                        ProjectList.Add (_reportView.Projects [i]);
                    }
                    donutChart.ReloadData ();

                } else if (_reportView.Projects.Count < ProjectList.Count) {

                    for (int i = 0; i < _reportView.Projects.Count; i++) {
                        ProjectList [i] = _reportView.Projects [i];
                    }
                    donutChart.ReloadData ();

                    for (int i = _reportView.Projects.Count; i < ProjectList.Count; i++) {
                        ProjectList.RemoveAt (i);
                    }
                    donutChart.ReloadData ();
                }

                projectTableView.ReloadData ();
            }
        }

        public List<ReportProject> ProjectList;

        public PieChart (RectangleF frame) : base (frame)
        {
            var pieCenter = new PointF (frame.Width / 2, frame.Height / 4);
            ProjectList = new List<ReportProject> ();

            donutChart = new XYDonutChart (new RectangleF (0, 0, frame.Width, frame.Height / 2)) {
                DataSource = this,
                PieCenter = pieCenter,
                PieRadius = pieChartRadius,
                UserInteractionEnabled = true,
                SelectedSliceStroke = 0,
                ShowPercentage = false,
                ShowLabel = true,
                AnimationSpeed = 1.0f,
                SelectedSliceOffsetRadius = 8f
            };
            Add (donutChart);

            projectTableView = new UITableView (new RectangleF (0, frame.Height / 2, frame.Width, frame.Height / 2));
            projectTableView.RegisterClassForCellReuse (typeof (ProjectReportCell), ProjectReportCell.ProjectReportCellId);
            projectTableView.Source = new ProjectListSource (this);
            Add (projectTableView);
        }

        XYDonutChart donutChart;

        const float pieChartRadius = 90f;
        UITableView projectTableView;


        public void SetSelectedProject (int index)
        {
            donutChart.SetSliceSelectedAtIndex (index);
        }

        public void SetDeselectedProject (int index)
        {
            donutChart.SetSliceDeselectedAtIndex (index);
        }


        #region Pie Datasource

        public int NumberOfSlicesInPieChart (XYDonutChart pieChart)
        {
            return ProjectList.Count;
        }

        public float ValueForSliceAtIndex (XYDonutChart pieChart, int index)
        {
            return ProjectList [index].TotalTime;
        }

        public UIColor ColorForSliceAtIndex (XYDonutChart pieChart, int index)
        {
            var hex = ProjectModel.HexColors [ProjectList [index].Color % ProjectModel.HexColors.Length];
            return UIColor.Clear.FromHex (hex);
        }

        public string TextForSliceAtIndex (XYDonutChart pieChart, int index)
        {
            return String.Empty;
        }

        #endregion

        internal class ProjectListSource : UITableViewSource
        {

            private List<ReportProject> _projectList;
            private PieChart _owner;

            public ProjectListSource (PieChart pieChart)
            {
                _projectList = pieChart.ProjectList;
                _owner = pieChart;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (ProjectReportCell)tableView.DequeueReusableCell (ProjectReportCell.ProjectReportCellId);
                cell.Data = _owner.ProjectList [indexPath.Row];
                return cell;
            }

            public override int RowsInSection (UITableView tableview, int section)
            {
                return _owner.ProjectList.Count;
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

