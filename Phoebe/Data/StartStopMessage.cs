using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data
{
    public class StartStopMessage : Message
    {
        public TimeEntryData TimeEntry
        {
            get {
                return (TimeEntryData)Sender;
            }
        }

        public StartStopMessage (TimeEntryData sender) : base (sender)
        {
        }
    }
}

