using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
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

        public Guid WorkspaceId { get; set; }
    }
}
