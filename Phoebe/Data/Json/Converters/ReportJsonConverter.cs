using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;
using System;
using System.Linq;
using System.Diagnostics;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ReportJsonConverter : BaseJsonConverter
    {
        private const string Tag = "ReportJsonConverter";

        public ReportJson Export (ReportData data)
        {
            return new ReportJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                TotalGrand = data.TotalGrand,
                TotalBillable = data.TotalBillable,
            };
        }

        private static ReportData ImportJson (ReportData data, ReportJson json)
        {
            data.ModifiedAt = json.ModifiedAt.ToUtc ();
            data.TotalGrand = json.TotalGrand;
            data.TotalBillable = json.TotalBillable;
            data.Activity = MakeActivityList (json.Activity);
            data.Projects = MakeProjectList (json.Projects);

            ImportCommonJson (data, json);
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
                    Project = item.Project,
                    TotalTime = item.TotalTime,
                    Color = item.Color,
                    BillableTime = item.Items.Where ( t => t.Sum > 0).Sum ( t => t.Time)
                };
                p.Items = new List<ReportTimeEntry> ();
                foreach (var i in item.Items) {
                    p.Items.Add ( new ReportTimeEntry() {
                        Rate = i.Rate,
                        Title = i.Title,
                        Time = i.Time,
                        Sum = i.Sum
                    });
                }
                projectList.Add (p);
            }
            return projectList;
        }

        private static List<ReportActivity> MakeActivityList (List<ReportRowJson> jsonList)
        {
            var activityList = new List<ReportActivity> ();
            foreach (var item in jsonList) {
                activityList.Add (new ReportActivity () {
                    StartTime = item.StartTime,
                    TotalTime = item.TotalTime,
                    BillableTime = item.BillableTime
                });
            }
            return activityList;
        }
    }
}
