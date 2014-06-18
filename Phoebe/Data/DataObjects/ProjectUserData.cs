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

        [ForeignRelation (typeof(ProjectData))]
        public Guid ProjectId { get; set; }

        [ForeignRelation (typeof(UserData))]
        public Guid UserId { get; set; }
    }
}
