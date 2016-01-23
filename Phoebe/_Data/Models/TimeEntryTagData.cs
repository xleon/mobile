using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    [Table ("TimeEntryTagModel")]
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
