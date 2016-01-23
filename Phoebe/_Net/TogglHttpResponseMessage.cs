using System;
using System.Net.Http;
using System.Net;

namespace Toggl.Phoebe._Net
{
    public class TogglHttpResponseMessage : Message
    {
        public TogglHttpResponseMessage (object sender, HttpResponseMessage resp, TimeSpan? latency = null) : base (sender)
        {
            StatusCode = resp.StatusCode;
            if (resp.Headers.Date.HasValue) {
                ServerTime = resp.Headers.Date.Value.UtcDateTime;
            }
            Latency = latency;
        }

        public HttpStatusCode StatusCode { get; private set; }

        public DateTime? ServerTime { get; private set; }

        public TimeSpan? Latency { get; private set; }
    }
}
