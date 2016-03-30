using System;
using SQLite.Net;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    public interface ITagData : ICommonData
    {
        string Name { get; }
        long WorkspaceRemoteId { get; }
        Guid WorkspaceId { get; }
        ITagData With (Action<TagData> transform);
    }

    [Table ("TagModel")]
    public class TagData : CommonData, ITagData
    {
        public TagData ()
        {
        }

        public TagData (string name, Guid workspaceId, long workspaceRemoteId)
        {
            Id = Guid.NewGuid ();
            Name = name;
            WorkspaceId = workspaceId;
            WorkspaceRemoteId = workspaceRemoteId;
            SyncState = SyncState.CreatePending;
        }

        protected TagData (TagData other) : base (other)
        {
            Name = other.Name;
            WorkspaceId = other.WorkspaceId;
            WorkspaceRemoteId = other.WorkspaceRemoteId;
        }

        public override object Clone ()
        {
            return new TagData (this);
        }

        public ITagData With (Action<TagData> transform)
        {
            return base.With (transform);
        }

        public string Name { get; set; }

        public long WorkspaceRemoteId { get; set; }

        public Guid WorkspaceId { get; set; }
    }
}
