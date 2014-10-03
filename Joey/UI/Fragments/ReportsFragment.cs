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

            LoadPieChart ();

            return view;
        }

        private async void LoadPieChart() {
            await LoadData();
            GeneratePieChart ();
        }

        private void GeneratePieChart()
        {
            foreach(var project in summaryReport.Projects)
            {
                var slice = new PieSlice ();
                slice.Value = project.TotalTime;
                slice.Color = Color.ParseColor( HexColors [project.Color % HexColors.Length]); 
                pieChart.AddSlice (slice);
            }
        }
        private async Task LoadData ()
        {
            summaryReport = new SummaryReportView ();
            summaryReport.Period = ZoomLevel.Month;
            await summaryReport.Load (1);
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            Console.WriteLine ("defaultWorkspaceId: {0}", user.DefaultWorkspaceId);
        }

        public class SliceListener : PieChart.IOnSliceClickedListener
        {
            public void OnClick (int index)
            {
                Console.WriteLine ("Onclick");
            }
        }

        public void GenerateProjectsList()
        {
            var adapter = new ProjectListAdapter (summaryReport.Projects);
            foreach (var row in summaryReport.Projects) {
                projectsList.AddView ();
            }
        }
    }

    public class ProjectListAdapter : BaseDataViewAdapter<object>
    {
    
        List<ReportProject> dataView;

        public ProjectListAdapter( List<ReportProject> dataView ) : base(dataView)
        {
        
        }

        private ProjectListAdapter (List<ReportProject> dataView) : base (dataView)
        {
            this.dataView = dataView;
        }

        protected override View GetModelView (ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                Resource.Layout.ReportsProjectListItem, parent, false);

            return view;
        }

    }


    private class ProjectListItemHolder
    {
        private TextView ProjectName;
        private TextView ProjectDuration;
        private View ColorSquare;

        public ProjectListItemHolder (View root) : base (root)
        {
            ProjectName = root.FindViewById<TextView> (Resource.Id.ProjectName).SetFont (Font.Roboto);
            ColorSquare = root.FindViewById<View> (Resource.Id.ColorSquare);
            ProjectDuration = root.FindViewById<TextView> (Resource.Id.ProjectDuration).SetFont(Font.Roboto);
        }

        protected override void Rebind ()
        {

        }
    }
}
