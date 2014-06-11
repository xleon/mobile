using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("ProjectUser")]
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
        }

        public bool IsManager { get; set; }

        public int HourlyRate { get; set; }

        public Guid ProjectId { get; set; }

        public Guid UserId { get; set; }
    }
}
