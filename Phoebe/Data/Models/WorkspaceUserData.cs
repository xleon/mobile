using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.Models
{
    public interface IWorkspaceUserData : ICommonData
    {
        bool IsAdmin { get; }
        bool IsActive { get; }
        string Email { get; }
        long WorkspaceRemoteId { get; }
        long UserRemoteId { get; }
        Guid WorkspaceId { get; }
        Guid UserId { get; }
        IWorkspaceUserData With (Action<WorkspaceUserData> transform);
    }

    [Table ("WorkspaceUserModel")]
    public class WorkspaceUserData : CommonData, IWorkspaceUserData
    {
        public WorkspaceUserData ()
        {
        }

        protected WorkspaceUserData (WorkspaceUserData other) : base (other)
        {
            IsAdmin = other.IsAdmin;
            IsActive = other.IsActive;
            WorkspaceId = other.WorkspaceId;
            UserId = other.UserId;
            Email = other.Email;
            WorkspaceRemoteId = other.WorkspaceRemoteId;
            UserRemoteId = other.UserRemoteId;
        }

        public override object Clone ()
        {
            return new WorkspaceUserData (this);
        }

        public IWorkspaceUserData With (Action<WorkspaceUserData> transform)
        {
            return base.With (transform);
        }

        public bool IsAdmin { get; set; }

        public bool IsActive { get; set; }

        public string Email { get; set; }

        public long WorkspaceRemoteId { get; set; }

        public long UserRemoteId { get; set; }

        public Guid WorkspaceId { get; set; }

        public Guid UserId { get; set; }
    }
}
