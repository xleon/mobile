using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("TaskModel")]
    public class TaskData : CommonData
    {
        public TaskData ()
        {
        }

        public TaskData (TaskData other) : base (other)
        {
            Name = other.Name;
            IsActive = other.IsActive;
            Estimate = other.Estimate;
            WorkspaceId = other.WorkspaceId;
            ProjectId = other.ProjectId;
        }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public long Estimate { get; set; }

        [ForeignRelation (typeof (WorkspaceData))]
        public Guid WorkspaceId { get; set; }

        [ForeignRelation (typeof (ProjectData))]
        public Guid ProjectId { get; set; }
    }
}
