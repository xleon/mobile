using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ReportJsonConverter
    {
        private const string Tag = "ReportJsonConverter";
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        private static ReportData ImportJson (ReportData data, ReportJson json)
        {
            data.TotalGrand = json.TotalGrand;
            data.TotalBillable = json.TotalBillable;
            data.Activity = MakeActivityList (json.ActivityContainer);
            data.Projects = MakeProjectList (json.Projects);
            return data;
        }

        public ReportData Import (ReportJson json)
        {
            return ImportJson (new ReportData (), json);
        }

        private static List<ReportProject> MakeProjectList (List<ReportProjectJson> jsonList)
        {
            var projectList = new List<ReportProject> ();

            foreach (var item in jsonList) {
                var p = new ReportProject () {
                    Project = item.Description.Project,
                    TotalTime = item.TotalTime,
                    Color = String.IsNullOrEmpty ( item.Description.Color) ? ProjectModel.HexColors.Length - 1 : int.Parse ( item.Description.Color),
                    BillableTime = item.Items.Where ( t => t.Sum > 0).Sum ( t => t.Time)
                };
                p.Items = new List<ReportTimeEntry> ();
                foreach (var i in item.Items) {
                    p.Items.Add ( new ReportTimeEntry() {
                        Rate = i.Rate,
                        Title = i.Description.Title,
                        Time = i.Time,
                        Sum = i.Sum
                    });
                }
                projectList.Add (p);
            }
            return projectList;
        }

        private static List<ReportActivity> MakeActivityList ( ReportActivityJson jsonList)
        {
            var activityList = new List<ReportActivity> ();
            foreach (var item in jsonList.Rows) {
                activityList.Add (new ReportActivity () {
                    StartTime = UnixStart.AddTicks ( ToLong (item[0]) * TimeSpan.TicksPerMillisecond),
                    TotalTime = ToLong ( item[1]),
                    BillableTime = ToLong (item[2])
                });
            }
            return activityList;
        }

        private static long ToLong (string s)
        {
            long l;
            long.TryParse (s, out l);
            return l;
        }
    }
}
