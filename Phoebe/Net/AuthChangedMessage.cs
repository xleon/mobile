
namespace Toggl.Phoebe.Net
{
    public class AuthChangedMessage : Message
    {
        private readonly AuthChangeReason reason;
        public AuthChangedMessage (AuthManager manager, AuthChangeReason reason) : base (manager)
        {
            this.reason = reason;
        }

        public AuthManager AuthManager
        {
            get { return (AuthManager)Sender; }
        }

        public AuthChangeReason Reason
        {
            get { return reason; }
        }
    }
}
