using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class ReportsRestClient : IReportsClient
    {
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
                var httpResp = await httpClient.SendAsync (httpReq, token).ConfigureAwait (false);
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

            var httpResp = await SendAsync (httpReq, token).ConfigureAwait (false);
            var respData = await httpResp.Content.ReadAsStringAsync ().ConfigureAwait (false);
            return JsonConvert.DeserializeObject<ReportJson> (respData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }

}