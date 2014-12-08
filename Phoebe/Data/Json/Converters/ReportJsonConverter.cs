using System;
using System.Collections.Generic;
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
            data.TotalCost = String.Format ("{0} {1}", json.TotalCurrencies [0].Amount, json.TotalCurrencies [0].Currency);
            return data;
        }

        public ReportData Import (ReportJson json)
        {
            return ImportJson (new ReportData (), json);
        }

        private static List<ReportProject> MakeProjectList (List<ReportProjectJson> jsonList)
        {
            var projectList = new List<ReportProject> ();
            int colorIndex;

            foreach (var item in jsonList) {
                var p = new ReportProject () {
                    Project = item.Description.Project,
                    TotalTime = item.TotalTime,
                    Color = Int32.TryParse (item.Description.Color, out colorIndex) ? colorIndex : ProjectModel.HexColors.Length - 1
                };
                p.Items = new List<ReportTimeEntry> ();
                if (item.Items != null) {
                    foreach (var i in item.Items) {
                        p.Items.Add (new ReportTimeEntry () {
                            Rate = i.Rate,
                            Title = i.Description.Title,
                            Time = i.Time,
                            Sum = i.Sum
                        });
                    }
                }
                p.Currencies = new List<ReportCurrency> ();
                if (item.Currencies != null) {
                    foreach (var i in item.Currencies) {
                        p.Currencies.Add (new ReportCurrency () {
                            Amount = i.Amount,
                            Currency = i.Currency
                        });
                    }
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
