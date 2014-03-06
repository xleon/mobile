using System;

namespace Toggl.Phoebe.Net
{
    public class AuthChangedMessage : Message
    {
        public AuthChangedMessage (AuthManager manager) : base (manager)
        {
        }

        public AuthManager AuthManager {
            get { return (AuthManager)Sender; }
        }
    }
}
