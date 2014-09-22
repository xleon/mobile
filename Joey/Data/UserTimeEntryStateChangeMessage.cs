using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Joey.Data
{
    public class UserTimeEntryStateChangeMessage : Message
    {
        private readonly TimeEntryData data;

        public UserTimeEntryStateChangeMessage (object sender, TimeEntryData data) : base (sender)
        {
            this.data = data;
        }

        public TimeEntryData Data
        {
            get { return data; }
        }
    }
}
