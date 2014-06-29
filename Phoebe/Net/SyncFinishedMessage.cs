using System;

namespace Toggl.Phoebe.Net
{
    public class SyncFinishedMessage : Message
    {
        private readonly SyncMode mode;
        private readonly bool hadErrors;
        private readonly Exception fatalError;

        public SyncFinishedMessage (ISyncManager sender, SyncMode mode, bool hadErrors, Exception fatalError) : base (sender)
        {
            this.mode = mode;
            this.hadErrors = hadErrors;
            this.fatalError = fatalError;
        }

        public ISyncManager SyncManager {
            get { return (ISyncManager)Sender; }
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
    