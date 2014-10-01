using System;
using Newtonsoft.Json;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Net;
using XPlatUtils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data.Views
{
    public class SummaryReportView
    {
        private static readonly string Tag = "SummaryReports";
        private DateTime StartDate;
        private ReportData dataObject;
        private DayOfWeek startOfWeek;
        private ZoomLevel period;

        public SummaryReportView ()
        {
        }

        public SummaryReportView (ZoomLevel p = ZoomLevel.Day)
        {
            period = p;
        }

        public async Task Load (int navigate = 0) // 1 last week, 2 - 2 weeks ago.
        {
            var current = DateTime.Today;

            if (period == ZoomLevel.Day) { // for week view.

                //Get start of the week.
                //                current.
                current.AddDays (-7); //start is week ago;
            }
        }

        public async Task Load (DateTime startDate)
        {
            StartDate = startDate;
            await Load ();
        }

        public async Task Load ()
        {
            if (IsLoading) {
                return;
            }

            try {
                /*
                IsLoading = true;
                var client = ServiceContainer.Resolve<ITogglClient> ();
                var user = ServiceContainer.Resolve<AuthManager> ().User;
                var store = ServiceContainer.Resolve<IDataStore> ();
                startOfWeek = user.StartOfWeek;
                var workspaceId = await store.ExecuteInTransactionAsync (ctx => ctx.GetRemoteId<WorkspaceData> (user.DefaultWorkspaceId));
                var endDate = StartDate.AddMonths (3);
                //var json = await client.GetReports (StartDate, endDate, (long)workspaceId);
                // dataObject = json.Import ();
                */

                dataObject = new ReportData();
                dataObject.Activity = new List<ReportActivity>() {
                    new ReportActivity() { StartTime = DateTime.Today, BillableTime = 222234, TotalTime = 122234 },
                    new ReportActivity() { StartTime = new DateTime ( DateTime.Today.Millisecond + 3600), BillableTime = 42234, TotalTime = 14434 },
                    new ReportActivity() { StartTime = new DateTime ( DateTime.Today.Millisecond + 5200), BillableTime = 52234, TotalTime = 22234 },
                    new ReportActivity() { StartTime = new DateTime ( DateTime.Today.Millisecond + 6700), BillableTime = 62234, TotalTime = 44234 },
                    new ReportActivity() { StartTime = new DateTime ( DateTime.Today.Millisecond + 8700), BillableTime = 32224, TotalTime = 12234 }
                };

                IsLoading = false;
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to fetch reports.");
            }
        }

        public bool IsLoading { get; private set; }

        public int ActivityCount
        {
            get {
                return dataObject.Activity.Count;
            }
        }

        public List<ReportActivity> activity
        {
            get {
                return dataObject.Activity;
            }
        }

        public string TotalBillale
        {
            get {
                var d = TimeSpan.FromMilliseconds (dataObject.TotalBillable);
                return String.Format ("{0}h {1}min", d.Hours, d.Minutes);
            }
        }

        public string TotalGrand
        {
            get {
                var d = TimeSpan.FromMilliseconds (dataObject.TotalGrand);
                return String.Format ("{0}h {1}min", d.Hours, d.Minutes);
            }
        }

        public List<string> ChartLabels ()
        {
            return new List<string> { "Mon", "Tue", "Whe", "Thu", "Fri", "Sat", "Sun" };
        }

        public List<string> ChartTimeLabels ()
        {
            var m = GetMaxPeriod ();
            List<string> labels = new List<string> ();
            for (int i = 1; i <= 4; i++) {
                labels.Add (String.Format ("{0}h", m / 4 * i));
            }
            return labels;
        }

        private int GetMaxPeriod ()
        {
            long max = 0;
            foreach (var s in dataObject.Activity) {
                max = max < s.TotalTime ? s.TotalTime : max;
            }
            return (int)Math.Ceiling ((double)TimeSpan.FromSeconds (max).Hours / 4D) * 4;
        }

        public ZoomLevel Period
        {
            get {
                return period;
            } set {
                period = value;
                Load ();
            }
        }

        private static string LabelForDate (DateTime date, ZoomLevel period)
        {
            /*
            if (period == ZoomLevel.Day)
                return date.DayOfWeek.ToString (); // Mon, Tue, Wed, Thu
            else if (period == ZoomLevel.Week)
                return date.Day; // 1, 2, 3??
            else
                return date.Month.ToString (); //Jan, Feb, Mar
            */

            return "HE";

        }
        //        private DateTime GetEndDate ()
        //        {
        //         summary->load("10 march", "monthly");
        //
        //         Week
        //         this week, last week... periods.
        //
        //         Month: From the start of the mont until to the end of the month.
        //         This month,last month... July 2014, june..
        //
        //         Year
        //         This year -> last year...
        //
        //        }
    }
}
