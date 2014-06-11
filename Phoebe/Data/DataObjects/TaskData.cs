using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("Task")]
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

        public Guid WorkspaceId { get; set; }

        public Guid ProjectId { get; set; }
    }
}
