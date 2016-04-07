using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe.Data.Json;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class ReportsRestClient : IReportsClient
    {
        private readonly Uri reportsv2Url;
        private CancellationTokenSource cts;

        public ReportsRestClient(Uri reportsApiUrl)
        {
            reportsv2Url = new Uri(reportsApiUrl, "v2/");
        }

        private HttpClient MakeHttpClient()
        {
            // Cannot share HttpClient instance between threads as it might (and will) cause InvalidOperationExceptions
            // occasionally.
            var client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(10),
            };
            var headers = client.DefaultRequestHeaders;
            headers.UserAgent.Clear();
            headers.UserAgent.Add(new ProductInfoHeaderValue(Platform.AppIdentifier, Platform.AppVersion));
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        private HttpRequestMessage SetupRequest(string authToken, HttpRequestMessage req)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(
                                               string.Format("{0}:api_token", authToken))));
            return req;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpReq)
        {
            using(var httpClient = MakeHttpClient())
            {
                var reqTimer = Stopwatch.StartNew();
                HttpResponseMessage httpResp;
                cts = new CancellationTokenSource();
                httpResp = await httpClient.SendAsync(httpReq, cts.Token).ConfigureAwait(false);
                reqTimer.Stop();
                PrepareResponse(httpResp, reqTimer.Elapsed);
                return httpResp;
            }
        }

        private void PrepareResponse(HttpResponseMessage resp, TimeSpan requestTime)
        {
            ServiceContainer.Resolve<MessageBus> ().Send(new TogglHttpResponseMessage(this, resp, requestTime));
            if (!resp.IsSuccessStatusCode)
            {
                throw new UnsuccessfulRequestException(resp.StatusCode, resp.ReasonPhrase);
            }
        }

        #region IReportClient implementation

        public async Task<ReportJson> GetReports(string authToken, long userRemoteId, DateTime startDate, DateTime endDate, long workspaceId)
        {
            var start = startDate.ToString("yyyy-MM-dd");
            var end = endDate.ToString("yyyy-MM-dd");
            var relUrl = "summary?billable=both&order_field=duration&order_desc=true&user_agent=toggl_mobile&subgrouping_ids=true&bars_count=31";
            relUrl = string.Format("{0}&since={1}&until={2}&user_ids={3}&workspace_id={4}", relUrl, start, end, userRemoteId, workspaceId);
            var url = new Uri(reportsv2Url, relUrl);

            var httpReq = SetupRequest(authToken, new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = url,
            });

            var httpResp = await SendAsync(httpReq).ConfigureAwait(false);
            var respData = await httpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ReportJson> (respData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        public void CancelRequest()
        {
            if (cts != null && cts.Token.CanBeCanceled)
            {
                cts.Cancel();
            }
        }

        public bool IsCancellationRequested
        {
            get
            {
                return cts.IsCancellationRequested;
            }
        }

        #endregion
    }

}