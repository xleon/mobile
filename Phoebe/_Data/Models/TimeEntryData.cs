using System;
using System.Collections.Generic;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    public enum TimeEntryState {
        New,
        Running,
        Finished
    }

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
            Tags = new List<string> (other.Tags);
        }

        public TimeEntryState State { get; set; }

        public string Description { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? StopTime { get; set; }

        public bool DurationOnly { get; set; }

        public bool IsBillable { get; set; }

        public Guid UserId { get; set; }

        public Guid WorkspaceId { get; set; }

        public Guid? ProjectId { get; set; }

        public Guid? TaskId { get; set; }

        public List<string> Tags { get; set; }
    }
}
