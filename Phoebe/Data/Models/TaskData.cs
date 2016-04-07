using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.Models
{
    public interface ITaskData : ICommonData
    {
        string Name { get; }
        bool IsActive { get; }
        long Estimate { get; }
        long WorkspaceRemoteId { get; }
        long ProjectRemoteId { get; }
        Guid WorkspaceId { get; }
        Guid ProjectId { get; }
        ITaskData With(Action<TaskData> transform);
    }

    [Table("TaskModel")]
    public class TaskData : CommonData, ITaskData
    {
        public static ITaskData Create(Action<TaskData> transform = null)
        {
            return CommonData.Create(transform);
        }

        /// <summary>
        /// ATTENTION: This constructor should only be used by SQL and JSON serializers
        /// To create new objects, use the static Create method instead
        /// </summary>
        public TaskData()
        {
        }

        TaskData(TaskData other) : base(other)
        {
            Name = other.Name;
            IsActive = other.IsActive;
            Estimate = other.Estimate;
            WorkspaceId = other.WorkspaceId;
            ProjectId = other.ProjectId;
            WorkspaceRemoteId = other.WorkspaceRemoteId;
            ProjectRemoteId = other.ProjectRemoteId;
        }

        public override object Clone()
        {
            return new TaskData(this);
        }

        public ITaskData With(Action<TaskData> transform)
        {
            return base.With(transform);
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
