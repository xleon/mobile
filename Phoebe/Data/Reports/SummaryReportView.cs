using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
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
        private long? workspaceId;
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

        private async Task Initialize ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            workspaceId = await store.ExecuteInTransactionAsync (ctx => ctx.GetRemoteId<WorkspaceData> (user.DefaultWorkspaceId));
            startOfWeek = user.StartOfWeek;
        }

        private async Task FetchData ()
        {
            dataObject = createEmtyReport ();
            try {
                _isError = false;
                var client = ServiceContainer.Resolve<IReportsClient> ();
                var json = await client.GetReports (startDate, endDate, (long)workspaceId);
                dataObject = json.Import ();
            } catch (Exception exc) {
                _isError = true;
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to fetch reports.");
            } finally {
                calculateReportData ();
            }
        }

        private void calculateReportData()
        {
            var user = ServiceContainer.Resolve<AuthManager> ().User;

            for (int i = 0; i < dataObject.Projects.Count; i++) {
                var project = dataObject.Projects[i];
                project.FormattedTotalTime = getFormattedTime (user, project.TotalTime);
                project.FormattedBillableTime = getFormattedTime (user, project.BillableTime);
                dataObject.Projects[i] = project;
            }

            long max = 0;
            foreach (var s in dataObject.Activity) {
                max = max < s.TotalTime ? s.TotalTime : max;
            }

            _maxTotal = (int)Math.Ceiling ( max/3600/ (double)5) * 5;

            _chartRowLabels = new List<string> ();
            foreach (var row in dataObject.Activity) {
                _chartRowLabels.Add (LabelForDate (row.StartTime));
            }

            _chartTimeLabels = new List<string> ();
            for (int i = 1; i <= 5; i++) {
                _chartTimeLabels.Add (String.Format ("{0} h", _maxTotal / 5 * i));
            }
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

        private ReportData createEmtyReport()
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