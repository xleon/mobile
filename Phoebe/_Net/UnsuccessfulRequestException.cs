using System;
using System.Net;
using System.Net.Http;

namespace Toggl.Phoebe._Net
{
    public class UnsuccessfulRequestException : HttpRequestException
    {
        private readonly HttpStatusCode statusCode;

        public UnsuccessfulRequestException (HttpStatusCode statusCode, string reasonPhrase)
        : base (String.Format ("{0} ({1})", (int)statusCode, reasonPhrase))
        {
            this.statusCode = statusCode;
        }

        public HttpStatusCode StatusCode
        {
            get { return statusCode; }
        }

        public bool IsValidationError
        {
            get { return statusCode == HttpStatusCode.BadRequest; }
        }

        public bool IsNonExistent
        {
            get { return statusCode == HttpStatusCode.NotFound; }
        }

        public bool IsForbidden
        {
            get { return statusCode == HttpStatusCode.Forbidden; }
        }
    }
}
