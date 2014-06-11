using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("Tag")]
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

        public Guid WorkspaceId { get; set; }
    }
}
