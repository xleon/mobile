
namespace Toggl.Phoebe.Net
{
    public class SyncStartedMessage : Message
    {
        private readonly SyncMode mode;

        public SyncStartedMessage (ISyncManager sender, SyncMode mode) : base (sender)
        {
            this.mode = mode;
        }

        public ISyncManager SyncManager
        {
            get { return (ISyncManager)Sender; }
        }

        public SyncMode Mode
        {
            get { return mode; }
        }
    }
}
