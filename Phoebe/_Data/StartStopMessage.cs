using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Data
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

