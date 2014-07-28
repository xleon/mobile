using System;
using XPlatUtils;
using System.Net.Http;
using System.Net;

namespace Toggl.Phoebe.Net
{
    public class TogglHttpResponseMessage : Message
    {
        public TogglHttpResponseMessage (object sender, HttpResponseMessage resp, TimeSpan? latency = null) : base (sender)
        {
            StatusCode = resp.StatusCode;
            Latency = latency;
        }

        public HttpStatusCode StatusCode { get; private set; }

        public TimeSpan? Latency { get; private set; }
    }
}
