using System;

namespace Toggl.Phoebe.Net
{
    public class AuthFailedMessage : Message
    {
        public enum Reason {
            InvalidCredentials,
            NetworkError,
            SystemError
        }

        private readonly Reason reason;
        private readonly Exception exception;

        public AuthFailedMessage (AuthManager manager, Reason reason, Exception ex = null) : base (manager)
        {
            this.reason = reason;
            this.exception = ex;
        }

        public Reason FailureReason
        {
            get { return reason; }
        }

        public Exception Exception
        {
            get { return exception; }
        }
    }
}
