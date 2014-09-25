using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
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

        [ForeignRelation (typeof (TimeEntryData))]
        public Guid TimeEntryId { get; set; }

        [ForeignRelation (typeof (TagData))]
        public Guid TagId { get; set; }
    }
}
