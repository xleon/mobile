using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("TimeEntryTag")]
    public class TimeEntryTagData : CommonData
    {
        public TimeEntryTagData ()
        {
        }

        public TimeEntryTagData (TimeEntryTagData other) : base (other)
        {
            TimeEntryId = other.TimeEntryId;
            TagId = other.TagId;
        }

        public Guid TimeEntryId { get; set; }

        public Guid TagId { get; set; }
    }
}
