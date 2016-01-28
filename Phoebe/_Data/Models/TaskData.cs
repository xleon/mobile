using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
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
            WorkspaceRemoteId = other.WorkspaceRemoteId;
            ProjectRemoteId = other.ProjectRemoteId;
        }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public long Estimate { get; set; }

        public long WorkspaceRemoteId { get; set; }

        public long ProjectRemoteId { get; set; }

        public Guid WorkspaceId { get; set; }

        public Guid ProjectId { get; set; }
    }
}
