using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    [Table ("WorkspaceUserModel")]
    public class WorkspaceUserData : CommonData
    {
        public WorkspaceUserData ()
        {
        }

        public WorkspaceUserData (WorkspaceUserData other) : base (other)
        {
            IsAdmin = other.IsAdmin;
            IsActive = other.IsActive;
            WorkspaceId = other.WorkspaceId;
            UserId = other.UserId;
        }

        public bool IsAdmin { get; set; }

        public bool IsActive { get; set; }

        public Guid WorkspaceId { get; set; }

        public Guid UserId { get; set; }
    }
}
