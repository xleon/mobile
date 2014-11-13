using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class ReportsRestClient : IReportsClient
    {
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private readonly Uri reportsv2Url;

        public ReportsRestClient (Uri reportsApiUrl)
        {
            reportsv2Url = new Uri (reportsApiUrl, "v2/");
        }

        private HttpClient MakeHttpClient ()
        {
            // Cannot share HttpClient instance between threads as it might (and will) cause InvalidOperationExceptions
            // occasionally.
            var client = new HttpClient () {
                Timeout = TimeSpan.FromSeconds (10),
            };
            var headers = client.DefaultRequestHeaders;
            headers.UserAgent.Clear ();
            headers.UserAgent.Add (new ProductInfoHeaderValue (Platform.AppIdentifier, Platform.AppVersion));
            headers.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

            return client;
        }

        private HttpRequestMessage SetupRequest (HttpRequestMessage req)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.Token != null) {
                req.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                        Convert.ToBase64String (Encoding.ASCII.GetBytes (
                                                    string.Format ("{0}:api_token", authManager.Token))));
            }
            return req;
        }

        private async Task<HttpResponseMessage> SendAsync (HttpRequestMessage httpReq, CancellationToken token)
        {
            using (var httpClient = MakeHttpClient ()) {

                var reqTimer = Stopwatch.StartNew ();
                var httpResp = await httpClient.SendAsync (httpReq, token)
                               .ConfigureAwait (continueOnCapturedContext: false);
                reqTimer.Stop ();
                //TODO: ask taavi about: PrepareResponse (httpResp, reqTimer.Elapsed);
                return httpResp;
            }
        }

        private void PrepareResponse (HttpResponseMessage resp, TimeSpan requestTime)
        {
            ServiceContainer.Resolve<MessageBus> ().Send (new TogglHttpResponseMessage (this, resp, requestTime));
            if (!resp.IsSuccessStatusCode) {
                throw new UnsuccessfulRequestException (resp.StatusCode, resp.ReasonPhrase);
            }
        }

        public async Task<ReportJson> GetReports (DateTime startDate, DateTime endDate, long workspaceId, CancellationToken token)
        {
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            var start = startDate.ToString ("yyyy-MM-dd");
            var end = endDate.ToString ("yyyy-MM-dd");
            var relUrl = "summary?billable=both&order_field=duration&order_desc=true&user_agent=toggl_mobile&subgrouping_ids=true&bars_count=31";
            relUrl = String.Format ("{0}&since={1}&until={2}&user_ids={3}&workspace_id={4}", relUrl, start, end, user.RemoteId, workspaceId);
            var url = new Uri (reportsv2Url, relUrl);

            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            });

            var httpResp = await SendAsync (httpReq, token)
                           .ConfigureAwait (continueOnCapturedContext: false);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                           .ConfigureAwait (continueOnCapturedContext: false);

            var json = JObject.Parse (respData);

            var RowList = new List<ReportRowJson> ();
            foreach (var row in json["activity"]["rows"]) {
                RowList.Add (new ReportRowJson () {
                    StartTime = UnixStart.AddTicks (ToLong (row [0]) * TimeSpan.TicksPerMillisecond),
                    TotalTime = ToLong (row [1]),
                    BillableTime = ToLong (row [2])
                });
            }

            var ProjectList = new List<ReportProjectJson> ();
            foreach (var row in json["data"]) {
                var newProject = new ReportProjectJson () {
                    Project = (string)row ["title"]["project"],
                    Client = (string)row ["title"] ["client"],
                    TotalTime = (long)row ["time"],
                    Color = row ["title"]["color"].ToObject<int?>(), // TODO: tricky solution?
                };
                var timeEntries = new List<ReportTimeEntryJson> ();
                foreach (var item in row["items"]) {
                    var timeEntry = new ReportTimeEntryJson() {
                        Title = (string)item["title"]["time_entry"],
                        Time = (long)item["time"],
                        Currency = (string)item["cur"],
                        //Sum = (float)item["sum"], // TODO: check how to read null values
                        //Rate = (float)item["rate"]
                    };
                    timeEntries.Add (timeEntry);
                }
                newProject.Items = timeEntries;
                ProjectList.Add (newProject);
            }

            return new ReportJson () {
                TotalGrand = ToLong (json ["total_grand"]),
                TotalBillable = ToLong (json ["total_billable"]),
                Activity = RowList,
                Projects = ProjectList,
            };
        }

        private long ToLong (JToken s)
        {
            long l;
            long.TryParse ((string)s, out l);
            return l;
        }
    }

}