using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    [Table ("ProjectUserModel")]
    public class ProjectUserData : CommonData
    {
        public ProjectUserData ()
        {
        }

        public ProjectUserData (ProjectUserData other) : base (other)
        {
            IsManager = other.IsManager;
            HourlyRate = other.HourlyRate;
            ProjectId = other.ProjectId;
            UserId = other.UserId;
            UserRemoteId = other.UserRemoteId;
            ProjectRemoteId = other.ProjectRemoteId;
        }

        public bool IsManager { get; set; }

        public int HourlyRate { get; set; }

        public long UserRemoteId { get; set; }

        public long ProjectRemoteId { get; set; }

        public Guid ProjectId { get; set; }

        public Guid UserId { get; set; }
    }
}
