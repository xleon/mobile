using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsFragment : ListFragment
    {
        private BarChart barChart;
        private PieChart pieChart;
        private TextView totalValue;
        private TextView billableValue;
        private ImageButton previousPeriod;
        private ImageButton nextPeriod;
        private TextView timePeriod;
        private SummaryReportView summaryReport;
        private int backDate;

        public ReportsFragment (int period)
        {
            backDate = period;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsFragment, container, false);

            barChart = view.FindViewById<BarChart> (Resource.Id.BarChart);
            pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);

            totalValue = view.FindViewById<TextView> (Resource.Id.TotalValue);
            billableValue = view.FindViewById<TextView> (Resource.Id.BillableValue);

            timePeriod = view.FindViewById<TextView> (Resource.Id.TimePeriodLabel);
            previousPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonPrevious);
            nextPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonNext);

            previousPeriod.Click += (sender, e) => NavigatePeriod (1);
            nextPeriod.Click += (sender, e) => NavigatePeriod (-1);

            LoadElements ();
            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ListView.SetClipToPadding (false);
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
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

        private void NavigatePeriod (int direction)
        {
            if (backDate == 0 && direction < 0) {
                backDate = 0;
            } else {
                backDate = backDate + direction;
            }
            LoadElements ();
        }

        private void OnSliceSelect (int position)
        {
            var adapter = ListView.Adapter as ProjectListAdapter;
            adapter.SetFocus (position);
        }

        private void EnsureAdapter ()
        {
            var adapter = new ProjectListAdapter (summaryReport.Projects);
            ListAdapter = adapter;
        }

        private async void LoadElements ()
        {
            await LoadData ();
            totalValue.Text = summaryReport.TotalGrand;
            billableValue.Text = summaryReport.TotalBillale;
            timePeriod.Text = FormattedDateSelector ();
            EnsureAdapter ();
            GeneratePieChart ();
            GenerateBarChart ();
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
            barChart.CeilingValue = summaryReport.GetCeilingSeconds ();
            barChart.SetBarTitles (summaryReport.ChartRowLabels ());
            barChart.SetLineTitles (summaryReport.ChartTimeLabels ());
            barChart.Refresh ();
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

        private async Task LoadData ()
        {
            summaryReport = new SummaryReportView ();
            summaryReport.Period = ZoomLevel.Week;
            await summaryReport.Load (backDate);
        }

        public string FormattedDateSelector ()
        {
            if (backDate == 0) {
                if (summaryReport.Period == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsThisWeek);
                } else if (summaryReport.Period == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsThisMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsThisYear);
                }
            } else if (backDate == 1) {
                if (summaryReport.Period == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsLastWeek);
                } else if (summaryReport.Period == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsLastMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsLastYear);
                }
            } else {
                var startDate = summaryReport.ResolveStartDate (backDate);
                var endDate = summaryReport.ResolveEndDate (startDate);
                if (summaryReport.Period == ZoomLevel.Week) {
                    return String.Format ("{0:MMM dd}th - {1:MMM dd}th", startDate, endDate);
                } else {
                    return "";
                }
            }
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

            projectName.Text = dataView [position].Project;
            projectDuration.Text = FormatMilliseconds (dataView [position].TotalTime);
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
            colorSquare.SetBackgroundDrawable (SquareDrawable);
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

        private string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            return String.Format ("{0}:{1:mm\\:ss}", Math.Floor (timeSpan.TotalHours).ToString ("00"), timeSpan);
        }
    }
}
