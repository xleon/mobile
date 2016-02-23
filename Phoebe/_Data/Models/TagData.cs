using System;
using SQLite.Net;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    [Table ("TagModel")]
    public class TagData : CommonData
    {
        public TagData ()
        {
        }

        public TagData (Guid workspaceId, string name)
        {
            Id = Guid.NewGuid ();
            Name = name;
            WorkspaceId = workspaceId;
        }

        public TagData (TagData other) : base (other)
        {
            Name = other.Name;
            WorkspaceId = other.WorkspaceId;
            WorkspaceRemoteId = other.WorkspaceRemoteId;
        }

        public string Name { get; set; }

        public long WorkspaceRemoteId { get; set; }

        public Guid WorkspaceId { get; set; }
    }
}
