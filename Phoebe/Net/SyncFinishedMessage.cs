using System;

namespace Toggl.Phoebe.Net
{
    public class SyncFinishedMessage : Message
    {
        private readonly SyncMode mode;
        private readonly bool hadErrors;
        private readonly Exception fatalError;

        public SyncFinishedMessage (SyncManager sender, SyncMode mode, bool hadErrors, Exception fatalError) : base (sender)
        {
            this.mode = mode;
            this.hadErrors = hadErrors;
            this.fatalError = fatalError;
        }

        public SyncManager SyncManager {
            get { return (SyncManager)Sender; }
        }

        public SyncMode Mode {
            get { return mode; }
        }

        public bool HadErrors {
            get { return hadErrors; }
        }

        public Exception FatalError {
            get { return fatalError; }
        }
    }
}
    