using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("TagModel")]
    public class TagData : CommonData
    {
        public TagData ()
        {
        }

        public TagData (TagData other) : base (other)
        {
            Name = other.Name;
            WorkspaceId = other.WorkspaceId;
        }

        public string Name { get; set; }

        [ForeignRelation (typeof(WorkspaceData))]
        public Guid WorkspaceId { get; set; }
    }
}
