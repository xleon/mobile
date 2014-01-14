using System;

namespace Toggl.Phoebe.Net
{
    public class SyncStartedMessage : Message
    {
        private readonly SyncMode mode;

        public SyncStartedMessage (SyncManager sender, SyncMode mode) : base (sender)
        {
            this.mode = mode;
        }

        public SyncManager SyncManager {
            get { return (SyncManager)Sender; }
        }

        public SyncMode Mode {
            get { return mode; }
        }
    }
}
