using System;

namespace Toggl.Phoebe.Net
{
    public class SyncWidgetMessage : Message
    {
        public SyncWidgetMessage (Object sender, bool isStarted = false) : base (sender)
        {
            this.isStarted = isStarted;
        }

        private bool isStarted;

        public bool IsStarted
        {
            get { return isStarted; }
        }
    }
}

