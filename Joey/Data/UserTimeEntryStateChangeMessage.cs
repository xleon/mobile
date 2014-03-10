using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey.Data
{
    public class UserTimeEntryStateChangeMessage : Message
    {
        private readonly TimeEntryModel model;

        public UserTimeEntryStateChangeMessage (object sender, TimeEntryModel model) : base (sender)
        {
            this.model = model;
        }

        public TimeEntryModel Model {
            get { return model; }
        }
    }
}

