using System;
using System.Collections.Generic;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Reports;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using ListFragment = Android.Support.V4.App.ListFragment;
using Android.Animation;

namespace Toggl.Joey.UI.Fragments
{
    public enum ChartPosition {
        Top = 0,
        Bottom = 1
    }

    public class ReportsFragment : ListFragment, View.IOnTouchListener
    {
        public event EventHandler PositionChanged;

        private BarChart barChart;
        private PieChart pieChart;
        private TextView totalValue;
        private TextView billableValue;
        private SummaryReportView summaryReport;
        private int backDate;
        private LinearLayout containerView;
        private float _viewY;
        private int topPosition;
        private int bottomPosition;
        private int contentHeight;
        private bool isLoading;

        private ChartPosition position;

        public ChartPosition Position
        {
            get {
                return position;
            } set {
                position = value;
                if (containerView != null) {
                    var currentPos = (position == ChartPosition.Top) ? topPosition : bottomPosition;
                    containerView.Layout (containerView.Left, currentPos, containerView.Right, currentPos + contentHeight);
                }
            }
        }

        public int Period
        {
            get {
                return backDate;
            }
        }

        public ZoomLevel ZoomLevel
        {
            get;
            set;
        }

        public bool IsClean
        {
            get;
            set;
        }

        public ReportsFragment (int period)
        {
            backDate = period;
            IsClean = true;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsFragment, container, false);
            containerView = view.FindViewById<LinearLayout> (Resource.Id.ReportsLayoutContainer);
            containerView.SetOnTouchListener (this);
            barChart = view.FindViewById<BarChart> (Resource.Id.BarChart);
            pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);

            containerView.LayoutChange += (sender, e) => {
                if ( contentHeight == 0) {
                    // set list size
                    var listView = view.FindViewById<ListView> (Android.Resource.Id.List);
                    var layoutParams = listView.LayoutParameters;
                    layoutParams.Height = View.Height - barChart.Height;
                    listView.LayoutParameters = layoutParams;

                    // define positions
                    topPosition = 0;
                    bottomPosition = - ( barChart.Height + ((ViewGroup.MarginLayoutParams)barChart.LayoutParameters).BottomMargin);

                    // set correct container size
                    contentHeight = barChart.Height + pieChart.Height + listView.Height;
                    layoutParams = containerView.LayoutParameters;
                    layoutParams.Height = contentHeight;
                    containerView.LayoutParameters = layoutParams;
                }
            };

            totalValue = view.FindViewById<TextView> (Resource.Id.TotalValue);
            billableValue = view.FindViewById<TextView> (Resource.Id.BillableValue);

            return view;
        }

        #region ListFragment

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ListView.SetClipToPadding (false);
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            var adapter = ListView.Adapter as ProjectListAdapter;
            if (pieChart.CurrentSlice == position) {
                pieChart.SelectSlice (-1);
                adapter.SetFocus (-1);
            } else {
                pieChart.SelectSlice (position);
                adapter.SetFocus (position);
            }
        }

        private void OnSliceSelect (int pos)
        {
            var adapter = ListView.Adapter as ProjectListAdapter;
            adapter.SetFocus (pos);
            if (pos != -1) {
                ListView.SmoothScrollToPositionFromTop (pos, 0);
            }
        }
        #endregion

        #region IOnGestureListener

        bool View.IOnTouchListener.OnTouch (View v, MotionEvent e)
        {
            switch (e.Action) {
            case MotionEventActions.Down:
                _viewY = e.RawY;
                break;
            case MotionEventActions.Move:
                var top = v.Top + (int) (e.RawY - _viewY);
                _viewY = e.RawY;
                if ( top <= topPosition && top >= bottomPosition) {
                    v.Layout (v.Left, top, v.Right, top + contentHeight);
                }
                break;
            case MotionEventActions.Up:
                var currentSnap = ( v.Top > (bottomPosition - topPosition) / 2) ? topPosition : bottomPosition;
                ValueAnimator animator = ValueAnimator.OfInt (v.Top, currentSnap);
                animator.SetDuration (250);
                animator.Start();
                animator.Update += (sender, ev) => {
                    int newValue = (int)ev.Animation.AnimatedValue;
                    v.Layout (v.Left, newValue, v.Right, newValue + contentHeight);
                    if ( newValue == currentSnap) {
                        position = ( currentSnap == topPosition) ? ChartPosition.Top : ChartPosition.Bottom;
                        if ( PositionChanged != null) {
                            PositionChanged.Invoke ( this, new EventArgs());
                        }
                    }
                };
                break;
            }
            return true;
        }

        #endregion

        public async void LoadElements ()
        {
            if ( IsClean) {
                isLoading = false;
                IsClean = true;
                summaryReport = new SummaryReportView ();
                summaryReport.Period = ZoomLevel;
                await summaryReport.Load (backDate);
                isLoading = false;
                IsClean = false;

                if (summaryReport.Activity != null) {
                    //totalValue.Text = summaryReport.TotalGrand;
                    //billableValue.Text = summaryReport.TotalBillale;
                    Position = position;
                    IsClean = false;
                }
            }

            return;

            ListAdapter = new ProjectListAdapter (summaryReport.Projects);
            GeneratePieChart ();
            GenerateBarChart ();
        }

        private void GenerateBarChart ()
        {
            return;

            barChart.Reset ();
            foreach (var row in summaryReport.Activity) {
                var bar = new BarItem ();
                bar.Value = (float)row.TotalTime;
                bar.Billable = (float)row.BillableTime;
                bar.Name = row.StartTime.ToShortTimeString ();
                barChart.AddBar (bar);
            }
            barChart.CeilingValue = summaryReport.MaxTotal;
            barChart.BarTitles = summaryReport.ChartRowLabels;
            barChart.LineTitles = summaryReport.ChartTimeLabels;
            barChart.Refresh ();
        }

        private void GeneratePieChart ()
        {
            return;

            pieChart.Reset ();
            var listener = new SliceListener ();
            pieChart.SetOnSliceClickedListener (listener);
            //pieChart.SliceClicked += OnSliceSelect;
            foreach (var project in summaryReport.Projects) {
                var slice = new PieSlice ();
                slice.Value = project.TotalTime;
                slice.Color = Color.ParseColor (ProjectModel.HexColors [project.Color % ProjectModel.HexColors.Length]);
                pieChart.AddSlice (slice);
            }
            pieChart.Refresh ();
        }

        public class SliceListener : PieChart.IOnSliceClickedListener
        {
            public void OnClick (int index)
            {
            }
        }
    }

    public class ProjectListAdapter : BaseAdapter
    {
        List<ReportProject> dataView;
        private View colorSquare;
        private TextView projectName;
        private TextView projectDuration;
        private int focus = -1;

        public ProjectListAdapter (List<ReportProject> dataView)
        {
            this.dataView = dataView;
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (Resource.Layout.ReportsProjectListItem, parent, false);
            projectName = view.FindViewById<TextView> (Resource.Id.ProjectName).SetFont (Font.Roboto);
            colorSquare = view.FindViewById<View> (Resource.Id.ColorSquare);
            projectDuration = view.FindViewById<TextView> (Resource.Id.ProjectDuration).SetFont (Font.Roboto);
            if (String.IsNullOrEmpty (dataView [position].Project)) {
                projectName.SetText (Resource.String.ReportsListViewNoProject);
            } else {
                projectName.Text = dataView [position].Project;
            }

            projectDuration.Text = dataView [position].FormattedTotalTime;
            var SquareDrawable = new GradientDrawable ();
            SquareDrawable.SetCornerRadius (5);
            SquareDrawable.SetColor (Color.ParseColor (ProjectModel.HexColors [dataView [position].Color % ProjectModel.HexColors.Length]));

            if (focus == position) {
                SquareDrawable.SetShape (ShapeType.Oval);
            } else if (focus != -1 && focus != position) {
                SquareDrawable.SetShape (ShapeType.Rectangle);
                SquareDrawable.SetAlpha (150);
                projectName.SetTextColor (Color.LightGray);
                projectDuration.SetTextColor (Color.LightGray);
            }
            return view;
        }

        public void SetFocus (int selected)
        {
            focus = selected;
            NotifyDataSetChanged ();
        }

        public override int Count
        {
            get {
                return dataView.Count;
            }
        }
    }
}
