using System;
using System.Collections.Generic;
using Android.Animation;
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

namespace Toggl.Joey.UI.Fragments
{
    public enum ChartPosition {
        Top = 0,
        Bottom = 1
    }

    public class ReportsFragment : Fragment, View.IOnTouchListener
    {
        private static readonly string ReportPeriodArgument = "com.toggl.timer.report_period";
        private static readonly string ReportZoomArgument = "com.toggl.timer.report_period";

        public event EventHandler PositionChanged;

        private BarChart barChart;
        private PieChart pieChart;
        private TextView totalValue;
        private ListView listView;
        private TextView billableValue;
        private SummaryReportView summaryReport;
        private LinearLayout containerView;
        private float _viewY;
        private float topPosition;
        private float bottomPosition;
        private bool isLoading;
        private int contentHeight;

        private ChartPosition position;

        public ChartPosition Position
        {
            get {
                return position;
            } set {
                if (position == value) { return; }
                position = value;
                if (containerView != null) {
                    containerView.RequestLayout ();
                }
            }
        }

        public int Period
        {
            get {
                int period = 0;
                if (Arguments != null) {
                    period = Arguments.GetInt (ReportPeriodArgument);
                }
                return period;
            }
        }

        public ZoomLevel ZoomLevel {

            get { 
                string zoomValue = ZoomLevel.Week.ToString ();
                if (Arguments != null) {
                    zoomValue = Arguments.GetString (ReportPeriodArgument, zoomValue);
                }
                return (ZoomLevel)Enum.Parse (typeof(ZoomLevel), zoomValue);
            }
        }

        public bool IsClean
        {
            get;
            set;
        }


        public ReportsFragment ()
        {
        }

        public ReportsFragment (int period, ZoomLevel zoom) : base()
        {
            var args = new Bundle ();
            args.PutInt (ReportPeriodArgument, period);
            args.PutString (ReportZoomArgument, zoom.ToString());

            Arguments = args;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsFragment, container, false);
            containerView = view.FindViewById<LinearLayout> (Resource.Id.ReportsLayoutContainer);
            containerView.SetOnTouchListener (this);
            barChart = view.FindViewById<BarChart> (Resource.Id.BarChart);
            pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);
            listView = view.FindViewById<ListView> (Resource.Id.ReportList);
            listView.ItemClick += OnListItemClick;

            containerView.LayoutChange += (sender, e) => {

                if ( topPosition.CompareTo ( bottomPosition) == 0) {
                    // define positions
                    topPosition = 0;
                    bottomPosition = -Convert.ToSingle ( pieChart.Top);
                    contentHeight =  view.Height + pieChart.Top; // it works! :)
                }

                // set position
                var currentPos = (position == ChartPosition.Top) ? topPosition : bottomPosition;
                containerView.SetY (currentPos);

                // set correct container size
                var layoutParams = containerView.LayoutParameters;
                layoutParams.Height = contentHeight;
                containerView.LayoutParameters = layoutParams;
                containerView.Layout ( containerView.Left, containerView.Top, containerView.Right, containerView.Top + contentHeight);
            };

            totalValue = view.FindViewById<TextView> (Resource.Id.TotalValue);
            billableValue = view.FindViewById<TextView> (Resource.Id.BillableValue);
            return view;
        }

        public override void OnStart ()
        {
            base.OnStart ();
            listView.LayoutMode = ViewLayoutMode.ClipBounds;
            listView.SetClipToPadding (false);
            IsClean = true;
        }

        #region ListFragment

        public void OnListItemClick ( object sender, AdapterView.ItemClickEventArgs args)
        {
            var adapter = listView.Adapter as ProjectListAdapter;
            if (pieChart.CurrentSlice == args.Position) {
                pieChart.SelectSlice (-1);
                adapter.SetFocus (-1);
            } else {
                pieChart.SelectSlice (args.Position);
                adapter.SetFocus (args.Position);
            }
        }

        private void OnSliceSelect (int pos)
        {
            var adapter = listView.Adapter as ProjectListAdapter;
            adapter.SetFocus (pos);
            if (pos != -1) {
                listView.SmoothScrollToPositionFromTop (pos, 0);
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
                var topY = v.GetY() + e.RawY - _viewY;
                if ( topY <= topPosition && topY >= bottomPosition) {
                    v.SetY (topY);
                }
                _viewY = e.RawY;
                break;
            case MotionEventActions.Up:
                var currentSnap = ( v.GetY() > (bottomPosition - topPosition) / 2) ? topPosition : bottomPosition;
                ValueAnimator animator = ValueAnimator.OfFloat (v.GetY(), currentSnap);
                animator.SetDuration (250);
                animator.Start();
                animator.Update += (sender, ev) => {
                    var newValue = (float)ev.Animation.AnimatedValue;
                    v.SetY ( newValue);
                    if ( newValue.CompareTo ( currentSnap) == 0) {
                        Position = ( currentSnap.CompareTo ( topPosition) == 0) ? ChartPosition.Top : ChartPosition.Bottom;
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
                summaryReport = new SummaryReportView ();
                summaryReport.Period = ZoomLevel;
                await summaryReport.Load (Period);
                isLoading = false;
                IsClean = false;

                if (summaryReport.Activity != null) {
                    totalValue.Text = summaryReport.TotalGrand;
                    billableValue.Text = summaryReport.TotalBillale;
                    listView.Adapter = new ProjectListAdapter ( Activity, summaryReport.Projects);
                    GeneratePieChart ();
                    GenerateBarChart ();
                    IsClean = false;
                }
            }
        }

        private void GenerateBarChart ()
        {
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

        protected void ChangeViewsPositions ( int posY)
        {
            listView.Layout (listView.Left, posY, listView.Right, posY + listView.Bottom);
            barChart.Layout (listView.Left, posY, listView.Right, posY + listView.Bottom);
            listView.Layout (listView.Left, posY, listView.Right, posY + listView.Bottom);
        }
    }

    public class ProjectListAdapter : BaseAdapter<ReportProject>
    {
        private List<ReportProject> dataView;
        private View colorSquare;
        private TextView projectName;
        private TextView projectDuration;
        private int focus = -1;
        private Android.App.Activity context;

        public ProjectListAdapter ( Android.App.Activity ctx, List<ReportProject> dataView)
        {
            this.dataView = dataView;
            context = ctx;
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        #region implemented abstract members of BaseAdapter

        public override ReportProject this [int index]
        {
            get {
                return dataView[index];
            }
        }

        #endregion

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? context.LayoutInflater.Inflate (Resource.Layout.ReportsProjectListItem, null);

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

        public override int Count {
            get {
                return dataView.Count;
            }
        }
    }
}
