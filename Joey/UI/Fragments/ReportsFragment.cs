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
using Toggl.Joey.UI.Adapters;
using System.Collections.Generic;
using Toggl.Joey.UI.Utils;
using Android.Graphics.Drawables;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsFragment : Fragment
    {
        private PieChart pieChart;
        private SummaryReportView summaryReport;
        private ListView projectsList;

        public static readonly string[] HexColors = {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsFragment, container, false);

            pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);
            projectsList = view.FindViewById<ListView> (Resource.Id.ReportsProjectList);

            pieChart.SetOnSliceClickedListener(new SliceListener());

            LoadPieChart ();

            return view;
        }
        private async void LoadPieChart() {
            await LoadData();
            GeneratePieChart ();
            GenerateProjectsList ();
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
            public void OnClick (int index) {}
        }

        public void GenerateProjectsList()
        {
            var adapter = new ProjectListAdapter (summaryReport.Projects);
            projectsList.Adapter = adapter;
        }

        private void OnItemClick (object sender, EventArgs e)
        {
            Console.WriteLine ("clicked");
        }
    }

    public class ProjectListAdapter : BaseAdapter
    {
        List<ReportProject> dataView;
        private View ColorSquare;
        private TextView ProjectName;
        private TextView ProjectDuration;

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

            var shape = ColorSquare.Background as GradientDrawable;
            if (shape != null) {
                shape.SetColor (Color.ParseColor (HexColors [dataView [position].Color % HexColors.Length]));
            }
            view.Click += OnItemClick;
            return view;
        }

        private void OnItemClick (object sender, EventArgs e)
        {
            var shape = ColorSquare.Background as GradientDrawable;
            if (shape != null) {
                shape.SetShape (ShapeType.Oval);
            }
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
