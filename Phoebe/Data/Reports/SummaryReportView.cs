using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Reports
{
    public class SummaryReportView
    {
        private static readonly string Tag = "SummaryReportsView";
        private DateTime startDate;
        private DateTime endDate;
        private ReportData dataObject;
        private DayOfWeek startOfWeek;
        private IReportsClient reportClient;
        private long? workspaceId;
        private List<ReportProject> pieChartProjects;
        private List<ReportProject> listChartProjects;

        public ZoomLevel Period;

        public async Task Load (int backDate)
        {
            if (IsLoading) {
                return;
            }
            IsLoading = true;

            if (workspaceId == null) {
                await Initialize ();
            }

            startDate = ResolveStartDate (backDate);
            endDate = ResolveEndDate (startDate);
            await FetchData ();
            IsLoading = false;
        }

        public void CancelLoad()
        {
            if (IsLoading ) {
                reportClient.CancelRequest ();
            }
        }

        private async Task Initialize ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            reportClient = ServiceContainer.Resolve<IReportsClient> ();
            workspaceId = await store.ExecuteInTransactionAsync (ctx => ctx.GetRemoteId<WorkspaceData> (user.DefaultWorkspaceId));
            startOfWeek = user.StartOfWeek;
        }

        private async Task FetchData ()
        {
            dataObject = CreateEmptyReport ();

            try {
                _isError = false;
                var json = await reportClient.GetReports (startDate, endDate, (long)workspaceId);
                dataObject = json.Import ();
            } catch ( Exception exc) {
                var log = ServiceContainer.Resolve<ILogger> ();
                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    var msg = (reportClient.IsCancellationRequested) ? "Fetch reports cancelation requested by user" : "Failed to fetch reports. Network failure.";
                    log.Info (Tag, exc, msg);
                } else {
                    log.Warning (Tag, exc, "Failed to fetch reports.");
                }
                _isError = ! (exc is TaskCanceledException);
            } finally {
                CalculateReportData ();
            }
        }

        private void CalculateReportData()
        {
            var user = ServiceContainer.Resolve<AuthManager> ().User;

            long max = 0;
            foreach (var s in dataObject.Activity) {
                max = max < s.TotalTime ? s.TotalTime : max;
            }

            _maxTotal = (int)Math.Ceiling (max / 3600f / 5f) * 5;

            _chartRowLabels = new List<string> ();
            foreach (var row in dataObject.Activity) {
                _chartRowLabels.Add (LabelForDate (row.StartTime));
            }

            _chartTimeLabels = new List<string> ();
            for (int i = 1; i <= 5; i++) {
                _chartTimeLabels.Add (String.Format ("{0} h", _maxTotal / 5 * i));
            }

            dataObject.Projects.Sort ((x, y) => y.TotalTime.CompareTo ( x.TotalTime));
            pieChartProjects = new List<ReportProject> ();
            listChartProjects = new List<ReportProject> ();

            var containerProject = new ReportProject {
                Currencies = new List<ReportCurrency>(),
                Color = ProjectModel.GroupedProjectColorIndex
            };

            const float minimunWeight = 0.01f; // minimum weight of project respect to total time
            var totalValue = Convert.ToSingle ( dataObject.Projects.Sum (p => p.TotalTime));
            int count = ProjectModel.GroupedProjectColorIndex;

            // group projects on one single project
            foreach (var item in dataObject.Projects) {
                if (Convert.ToSingle (item.TotalTime) / totalValue > minimunWeight) {
                    pieChartProjects.Add (item);
                } else {
                    containerProject.BillableTime += item.BillableTime;
                    containerProject.TotalTime += item.TotalTime;

                    // group currencies
                    foreach (var currencyItem in item.Currencies) {
                        var index = containerProject.Currencies.FindIndex (c => c.Currency == currencyItem.Currency);
                        if (index != -1)
                            containerProject.Currencies [index] = new ReportCurrency {
                            Amount = containerProject.Currencies [index].Amount + currencyItem.Amount,
                            Currency = currencyItem.Currency
                        };
                        else {
                            containerProject.Currencies.Add ( currencyItem);
                        }
                    }
                    count++;
                }
            }

            // check if small projects exists and are enough to be a separeted slice
            if (containerProject.TotalTime > 0 && Convert.ToSingle (containerProject.TotalTime) / totalValue > minimunWeight) {
                containerProject.Project = count.ToString();


                pieChartProjects.Add (containerProject);
                listChartProjects = new List<ReportProject> (pieChartProjects);
            } else {
                listChartProjects = new List<ReportProject> (dataObject.Projects);
            }

            // format total and billable time
            FormatTimeData (pieChartProjects, user);
            FormatTimeData (listChartProjects, user);
            FormatTimeData (dataObject.Projects, user);
        }

        public bool IsLoading { get; private set; }


        public int ActivityCount
        {
            get {
                return dataObject.Activity.Count;
            }
        }

        public List<ReportActivity> Activity
        {
            get {
                return dataObject.Activity;
            }
        }

        public List<ReportProject> Projects
        {
            get {
                return dataObject.Projects;
            }
        }

        public List<ReportProject> PieChartProjects
        {
            get {
                return pieChartProjects;
            }
        }

        public List<ReportProject> ListChartProjects
        {
            get {
                return listChartProjects;
            }
        }

        public List<string> TotalCost
        {
            get {
                return dataObject.TotalCost;
            }
        }

        public string TotalBillale
        {
            get {
                return FormatMilliseconds (dataObject.TotalBillable);
            }
        }

        public string TotalGrand
        {
            get {
                return FormatMilliseconds (dataObject.TotalGrand);
            }
        }

        private List<string> _chartRowLabels;

        public List<string> ChartRowLabels
        {
            get {
                return _chartRowLabels;
            }
        }

        private List<string> _chartTimeLabels;

        public List<string> ChartTimeLabels
        {
            get {
                return _chartTimeLabels;
            }
        }

        private int _maxTotal;

        public int MaxTotal
        {
            get {
                return _maxTotal;
            }
        }

        private bool _isError;

        public bool IsError
        {
            get {
                return _isError;
            }
        }

        public string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            decimal totalHours = Math.Floor ((decimal)timeSpan.TotalHours);
            return String.Format ("{0} h {1} min", (int)totalHours, timeSpan.Minutes);
        }

        public DateTime ResolveStartDate (int backDate)
        {
            var current = DateTime.Today;
            if (Period == ZoomLevel.Week) {
                var date = current.StartOfWeek (startOfWeek).AddDays (backDate * 7);
                return date;
            }

            if (Period == ZoomLevel.Month) {
                current = current.AddMonths (backDate);
                return new DateTime (current.Year, current.Month, 1);
            }

            return new DateTime (current.Year + backDate, 1, 1);
        }

        public DateTime ResolveEndDate (DateTime start)
        {
            if (Period == ZoomLevel.Week) {
                return start.AddDays (6);
            }

            if (Period == ZoomLevel.Month) {
                return start.AddMonths (1).AddDays (-1);
            }

            return start.AddYears (1).AddDays (-1);
        }

        public List<ReportProject> GetProjectsByAngle ( float minimunAngle)
        {
            if (dataObject == null) {
                return new List<ReportProject> ();
            }

            float angle = minimunAngle / 360f; // angle in degrees
            var totalValue = Convert.ToSingle ( dataObject.Projects.Sum (p => p.TotalTime));
            return dataObject.Projects.Where (p => Convert.ToSingle ( p.TotalTime) / totalValue > angle).ToList ();
        }

        public static ZoomLevel GetLastZoomViewed()
        {
            var settings = ServiceContainer.Resolve<ISettingsStore> ();
            var value = settings.LastReportZoomViewed ?? (int)ZoomLevel.Week;
            return (ZoomLevel)value;
        }

        public static void SaveReportsState ( ZoomLevel zoomLevel)
        {
            var settings = ServiceContainer.Resolve<ISettingsStore> ();
            settings.LastReportZoomViewed = (int)zoomLevel;
        }

        private string LabelForDate (DateTime date)
        {
            if (Period == ZoomLevel.Week) {
                return String.Format ("{0:ddd}", date);
            }

            if (Period == ZoomLevel.Month) {
                return String.Format ("{0:ddd dd}", date);
            }

            return String.Format ("{0:MMM}", date);
        }

        private string getFormattedTime ( UserData user, long totalTime)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds ( totalTime);
            string formattedString = duration.ToString (@"h\:mm\:ss");

            if ( user!= null) {
                if ( user.DurationFormat == DurationFormat.Classic) {
                    if (duration.TotalMinutes < 1) {
                        formattedString = duration.ToString (@"s\ \s\e\c");
                    } else if (duration.TotalMinutes > 1 && duration.TotalMinutes < 60) {
                        formattedString = duration.ToString (@"mm\:ss\ \m\i\n");
                    } else {
                        formattedString = duration.ToString (@"hh\:mm\:ss");
                    }
                } else if (user.DurationFormat == DurationFormat.Decimal) {
                    formattedString = String.Format ("{0:0.00} h", duration.TotalHours);
                }
            }
            return formattedString;
        }

        private void FormatTimeData ( IList<ReportProject> items, UserData user)
        {
            for (int i = 0; i < items.Count; i++) {
                var project = items[i];
                project.FormattedTotalTime = getFormattedTime (user, project.TotalTime);
                project.FormattedBillableTime = getFormattedTime (user, project.BillableTime);
                items[i] = project;
            }
        }

        private ReportData CreateEmptyReport()
        {
            var activityList = new List<ReportActivity> ();

            int total;
            if (Period == ZoomLevel.Week) {
                total = 7;
            } else if (Period == ZoomLevel.Month) {
                total = 30;
            } else {
                total = 12;
            }

            for (int i = 0; i < total; i++) {
                var activiy = new ReportActivity ();
                activiy.BillableTime = 0;
                activiy.TotalTime = 0;
                if (Period == ZoomLevel.Week) {
                    activiy.StartTime = startDate.AddDays (Convert.ToDouble (i));
                } else if (Period == ZoomLevel.Month) {
                    activiy.StartTime = startDate.AddDays (Convert.ToDouble (i));
                } else {
                    activiy.StartTime = startDate.AddMonths (i);
                }
                activityList.Add (activiy);
            }

            return new ReportData () {
                Projects = new List<ReportProject>(),
                Activity = activityList,
                TotalBillable = 0,
                TotalGrand = 0
            };
        }
    }
}
