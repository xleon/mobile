using System;

namespace Toggl.Phoebe.Net
{
    public class AuthFailedMessage : Message
    {
        private readonly AuthResult result;
        private readonly Exception exception;

        public AuthFailedMessage (AuthManager manager, AuthResult result, Exception ex = null) : base (manager)
        {
            this.result = result;
            this.exception = ex;
        }

        public AuthResult Result
        {
            get { return result; }
        }

        public Exception Exception
        {
            get { return exception; }
        }
    }
}
