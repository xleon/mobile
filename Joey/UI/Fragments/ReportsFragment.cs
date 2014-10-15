using System;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toggl.Phoebe.Data.Reports;
using Toggl.Phoebe.Data;
using System.Threading.Tasks;
using XPlatUtils;
using Toggl.Phoebe.Net;
using Android.Graphics;
using ListFragment = Android.Support.V4.App.ListFragment;
using Android.Widget;
using System.Collections.Generic;
using Toggl.Joey.UI.Utils;
using Android.Graphics.Drawables;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsFragment : ListFragment
    {
        private PieChart pieChart;
        private SummaryReportView summaryReport;

        public static readonly string[] HexColors = {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsFragment, container, false);
            pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);
            var d = new SliceListener ();
            pieChart.SetOnSliceClickedListener(d);
            pieChart.SliceClicked += new SliceClickedEventHandler (OnSliceSelect); 

            LoadPieChart ();
            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ListView.SetClipToPadding (false);
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            Console.WriteLine ("list item clicked!");
            pieChart.SelectSlice (position);
            var adapter = ListView.Adapter as ProjectListAdapter;
            adapter.SetFocus (position);
            if (adapter == null) {
                return;
            }

            var model = adapter.GetItem (position);
            if (model == null) {
                return;
            }
        }

        private void OnSliceSelect(int position){
            var adapter = ListView.Adapter as ProjectListAdapter;
            adapter.SetFocus (position);

        }
        private void EnsureAdapter ()
        {
            // check, if adapter exists, if it does, dont overwrite.
            var adapter = new ProjectListAdapter (summaryReport.Projects);
            ListAdapter = adapter;
        }

        private async void LoadPieChart() {
            await LoadData();
            EnsureAdapter ();
            GeneratePieChart ();
        }

        private void GeneratePieChart()
        {
            pieChart.IsLoading = true;

            foreach(var project in summaryReport.Projects)
            {
                var slice = new PieSlice ();
                slice.Value = project.TotalTime;
                slice.Color = Color.ParseColor( HexColors [project.Color % HexColors.Length]); 
                pieChart.AddSlice (slice);
            }
            pieChart.IsLoading = false;
        }

        private async Task LoadData ()
        {
            summaryReport = new SummaryReportView ();
            summaryReport.Period = ZoomLevel.Month;
            await summaryReport.Load (0);
            var user = ServiceContainer.Resolve<AuthManager> ().User;
        }

        public class SliceListener : PieChart.IOnSliceClickedListener
        {
            public void OnClick (int index) {
                Console.WriteLine ("slice clicked");
            }
        }
    }

    public class ProjectListAdapter : BaseAdapter
    {
        List<ReportProject> dataView;
        private View ColorSquare;
        private TextView ProjectName;
        private TextView ProjectDuration;
        private int focus = -1;

        public static readonly string[] HexColors = {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

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
            ProjectName = view.FindViewById<TextView> (Resource.Id.ProjectName).SetFont (Font.Roboto);
            ColorSquare = view.FindViewById<View> (Resource.Id.ColorSquare);
            ProjectDuration = view.FindViewById<TextView> (Resource.Id.ProjectDuration).SetFont(Font.Roboto);

            ProjectName.Text = dataView [position].Project;
            ProjectDuration.Text = FormatMilliseconds(dataView [position].TotalTime);
            var SquarDrawable = new GradientDrawable ();
            SquarDrawable.SetCornerRadius (5);
            SquarDrawable.SetColor (Color.ParseColor (HexColors [dataView [position].Color % HexColors.Length]));

            if (focus == position) {
                SquarDrawable.SetShape (ShapeType.Oval);
            } else if (focus != -1 && focus != position) {
                SquarDrawable.SetShape (ShapeType.Rectangle);
                SquarDrawable.SetAlpha (150);
                ProjectName.SetTextColor (Color.LightGray);
                ProjectDuration.SetTextColor (Color.LightGray);
            }
            ColorSquare.SetBackgroundDrawable (SquarDrawable);
            return view;
        }

        public void SetFocus(int selected)
        {
            focus = selected;
            NotifyDataSetChanged ();
        }

        public override int Count {
            get {
                return dataView.Count;
            }
        }

        private string FormatMilliseconds(long ms)
        {
            var timeSpan =  TimeSpan.FromMilliseconds (ms);
            return String.Format ("{0}:{1:mm\\:ss}", Math.Floor(timeSpan.TotalHours).ToString("00"), timeSpan);
        }
    }
}
