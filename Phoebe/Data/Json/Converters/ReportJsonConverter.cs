using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;
using System;

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
//                Activity = data.Activity,
//                Projects = data.Projects
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
                projectList.Add (new ReportProject () {
                    Project = item.Project,
                    TotalTime = item.TotalTime
                });
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
