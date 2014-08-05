using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Toggl.Phoebe.Net
{
    public class PushRestClient : IPushClient
    {
        private readonly Uri v8Url;
        private readonly HttpClient httpClient;

        public PushRestClient (Uri url)
        {
            v8Url = new Uri (url, "v8/push_service/");
            httpClient = new HttpClient () {
                Timeout = TimeSpan.FromSeconds (10),
            };
            var headers = httpClient.DefaultRequestHeaders;
            headers.UserAgent.Clear ();
            headers.UserAgent.Add (new ProductInfoHeaderValue (Platform.AppIdentifier, Platform.AppVersion));
            headers.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));
        }

        private string GetPayload (PushService service, string regid)
        {
            switch (service) {
            case PushService.GCM:
                return new JObject (
                    new JProperty ("pushservicetype", "gcm"),
                    new JProperty ("regid", regid)
                ).ToString ();
            case PushService.APNS:
                return new JObject (
                    new JProperty ("pushservicetype", "apns"),
                    new JProperty ("devtoken", regid)
                ).ToString ();
            default:
                throw new ArgumentException ("Unknown service", "service");
            }
        }

        private void AddAuthToken (string authToken, HttpRequestMessage req)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                Convert.ToBase64String (ASCIIEncoding.ASCII.GetBytes (
                    string.Format ("{0}:api_token", authToken))));
        }

        public Task Register (string authToken, PushService service, string regid)
        {
            var url = new Uri (v8Url, "subscribe");
            string json = GetPayload (service, regid);

            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            };
            AddAuthToken (authToken, httpReq);

            return httpClient.SendAsync (httpReq);
        }

        public Task Unregister (string authToken, PushService service, string regid)
        {
            var url = new Uri (v8Url, "unsubscribe");
            string json = GetPayload (service, regid);

            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            };
            AddAuthToken (authToken, httpReq);

            return httpClient.SendAsync (httpReq);
        }
    }
}
