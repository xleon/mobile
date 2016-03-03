using System;

namespace Toggl.Phoebe.Net
{
    public class UpdateStartedMessage : Message
    {
        private readonly DateTime startDate;

        public UpdateStartedMessage (ISyncManager sender, DateTime startDate) : base (sender)
        {
            this.startDate = startDate;
        }

        public ISyncManager SyncManager
        {
            get { return (ISyncManager)Sender; }
        }

        public DateTime StartDate
        {
            get { return startDate; }
        }
    }

    public class UpdateFinishedMessage : Message
    {
        private readonly DateTime startDate;
        private readonly DateTime endDate;
        private readonly bool hadErrors;
        private readonly bool hadMore;

        public UpdateFinishedMessage (ISyncManager sender, DateTime startDate, DateTime endDate, bool hadMore, bool hadErrors) : base (sender)
        {
            this.hadErrors = hadErrors;
            this.startDate = startDate;
            this.endDate = endDate;
            this.hadMore = hadMore;
        }

        public ISyncManager SyncManager
        {
            get { return (ISyncManager)Sender; }
        }

        public DateTime StartDate
        {
            get { return startDate; }
        }

        public DateTime EndDate
        {
            get { return endDate; }
        }

        public bool HadErrors
        {
            get { return hadErrors; }
        }

        public bool HadMore
        {
            get { return hadMore; }
        }
    }
}