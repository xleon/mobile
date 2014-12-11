using System;
using System.Collections.Generic;
using Android.Animation;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Reports;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public enum ChartPosition {
        Top = 0,
        Bottom = 1
    }

    public class ReportsFragment : Fragment
    {
        private static readonly string ReportPeriodArgument = "com.toggl.timer.report_period";
        private static readonly string ReportZoomArgument = "com.toggl.timer.report_zoom";

        public event EventHandler PositionChanged;

        private SnappyLayout snappyLayout;
        private BarChart barChart;
        private PieChart pieChart;
        private TextView totalValue;
        private ListView listView;
        private TextView billableValue;
        private SummaryReportView summaryReport;
        private bool isLoading;

        private ChartPosition position;

        public ChartPosition Position
        {
            get {
                return position;
            } set {
                if (position == value) {
                    return;
                }
                position = value;

                if (snappyLayout != null) {
                    snappyLayout.ActiveChild = (int)position;
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

        public ZoomLevel ZoomLevel
        {

            get {
                string zoomValue = ZoomLevel.Week.ToString ();
                if (Arguments != null) {
                    zoomValue = Arguments.GetString (ReportZoomArgument, zoomValue);
                }
                return (ZoomLevel)Enum.Parse (typeof (ZoomLevel), zoomValue);
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
            snappyLayout = view.FindViewById<SnappyLayout> (Resource.Id.SnappyLayout);
            snappyLayout.ActiveChild = (int)position;
            snappyLayout.ActiveChildChanged += OnSnappyActiveChildChanged;
            barChart = view.FindViewById<BarChart> (Resource.Id.BarChart);
            pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);
            listView = view.FindViewById<ListView> (Resource.Id.ReportList);
            listView.ItemClick += OnListItemClick;

            totalValue = view.FindViewById<TextView> (Resource.Id.TotalValue);
            billableValue = view.FindViewById<TextView> (Resource.Id.BillableValue);

            return view;
        }

        private void OnSnappyActiveChildChanged (object sender, EventArgs e)
        {
            var value = (ChartPosition)snappyLayout.ActiveChild;
            if (position == value) {
                return;
            }

            position = value;
            if (PositionChanged != null) {
                PositionChanged (this, EventArgs.Empty);
            }
        }

        public override void OnStart ()
        {
            base.OnStart ();
            listView.LayoutMode = ViewLayoutMode.ClipBounds;
            listView.SetClipToPadding (false);
            IsClean = true;

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Reports";
        }

        #region ListFragment

        public void OnListItemClick ( object sender, AdapterView.ItemClickEventArgs args)
        {
            var adapter = listView.Adapter as ReportProjectAdapter;
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
            var adapter = listView.Adapter as ReportProjectAdapter;
            adapter.SetFocus (pos);
            if (pos != -1) {
                listView.SmoothScrollToPositionFromTop (pos, 0);
            }
        }

        #endregion

        public async void LoadElements ()
        {
            if ( IsClean) {
                isLoading = true;
                summaryReport = new SummaryReportView ();
                summaryReport.Period = ZoomLevel;
                await summaryReport.Load (Period);
                isLoading = false;
                IsClean = false;

                if (summaryReport.Activity != null) {
                    totalValue.Text = summaryReport.TotalGrand;
                    billableValue.Text = summaryReport.TotalBillale;
                    listView.Adapter = new ReportProjectAdapter ( Activity, summaryReport.Projects);
                    GeneratePieChart ();
                    barChart.Reset (summaryReport);
                    IsClean = false;
                }
            }
        }

        public void SetZoomLevel ( ZoomLevel zoomlevel)
        {
            Arguments.PutString (ReportZoomArgument, zoomlevel.ToString());
        }

        private void GeneratePieChart ()
        {
            pieChart.Reset ();
            var listener = new SliceListener ();
            pieChart.SetOnSliceClickedListener (listener);
            pieChart.SliceClicked += OnSliceSelect;
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

        private class ReportProjectAdapter : BaseAdapter<ReportProject>
        {
            private List<ReportProject> dataView;
            private View colorSquare;
            private TextView projectName;
            private TextView projectDuration;
            private int focus = -1;
            private Android.App.Activity context;

            public ReportProjectAdapter ( Android.App.Activity ctx, List<ReportProject> dataView)
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
                View view = convertView;

                if (convertView == null) {
                    view = context.LayoutInflater.Inflate (Resource.Layout.ReportsProjectListItem, parent, false);
                    view.Tag = new ProjectViewHolder (view);
                }
                var holder = (ProjectViewHolder)view.Tag;
                holder.Bind (dataView [position]);
                holder.SetFocus (focus, position); // mmm...

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

            private class ProjectViewHolder : BindableViewHolder<ReportProject>
            {
                private View _root;

                public View ColorSquareView  { get; private set; }

                public TextView NameTextView  { get; private set; }

                public TextView DurationTextView  { get; private set; }

                public ProjectViewHolder ( View root)  : base (root)
                {
                    NameTextView = root.FindViewById<TextView> (Resource.Id.ProjectName).SetFont (Font.Roboto);

                    ColorSquareView = root.FindViewById<View> (Resource.Id.ColorSquare);

                    DurationTextView = root.FindViewById<TextView> (Resource.Id.ProjectDuration).SetFont (Font.Roboto);

                    _root = root;
                }

                protected override void Rebind ()
                {
                    if (String.IsNullOrEmpty ( DataSource.Project)) {
                        NameTextView.SetText (Resource.String.ReportsListViewNoProject);
                    } else {
                        NameTextView.Text = DataSource.Project;
                    }

                    DurationTextView.Text = DataSource.FormattedTotalTime;
                    var squareDrawable = new GradientDrawable ();
                    squareDrawable.SetCornerRadius (5);
                    squareDrawable.SetColor (Color.ParseColor (ProjectModel.HexColors [ DataSource.Color % ProjectModel.HexColors.Length]));
                    ColorSquareView.SetBackgroundDrawable (squareDrawable);
                }

                public void SetFocus ( int focus, int position )
                {
                    var squareDrawable = (GradientDrawable)ColorSquareView.Background;
                    if (focus != -1) {
                        _root.Alpha = (focus == position) ? 1 : 0.5f;
                        var radius = (focus == position) ? Convert.ToSingle ( ColorSquareView.Height / 2) : 5.0f;
                        squareDrawable.SetCornerRadius ( radius);
                    } else {
                        _root.Alpha = 1;
                        squareDrawable.SetCornerRadius (5);
                    }
                }
            }
        }
    }
}
