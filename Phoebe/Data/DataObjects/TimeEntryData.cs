using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("TimeEntryModel")]
    public class TimeEntryData : CommonData
    {
        public TimeEntryData ()
        {
            State = TimeEntryState.New;
        }

        public TimeEntryData (TimeEntryData other) : base (other)
        {
            State = other.State;
            Description = other.Description;
            StartTime = other.StartTime;
            StopTime = other.StopTime;
            DurationOnly = other.DurationOnly;
            IsBillable = other.IsBillable;
            UserId = other.UserId;
            WorkspaceId = other.WorkspaceId;
            ProjectId = other.ProjectId;
            TaskId = other.TaskId;
        }

        public TimeEntryState State { get; set; }

        public string Description { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? StopTime { get; set; }

        public bool DurationOnly { get; set; }

        public bool IsBillable { get; set; }

        [ForeignRelation (typeof (UserData))]
        public Guid UserId { get; set; }

        [ForeignRelation (typeof (WorkspaceData))]
        public Guid WorkspaceId { get; set; }

        [ForeignRelation (typeof (ProjectData))]
        public Guid? ProjectId { get; set; }

        [ForeignRelation (typeof (TaskData))]
        public Guid? TaskId { get; set; }
    }
}
