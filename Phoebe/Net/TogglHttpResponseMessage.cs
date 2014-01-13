using System;
using XPlatUtils;
using System.Net.Http;
using System.Net;

namespace Toggl.Phoebe.Net
{
    public class TogglHttpResponseMessage : Message
    {
        public TogglHttpResponseMessage (object sender, HttpResponseMessage resp) : base (sender)
        {
            StatusCode = resp.StatusCode;
        }

        public HttpStatusCode StatusCode { get; private set; }
    }
}
