using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TogglDoodle.Models
{
    public class TimeEntry : Model
    {
        public string Description { get; set; }

        public long? WorkspaceId { get; set; }

        public long? ProjectId { get; set; }

        public long? TaskId { get; set; }

        public bool Billable { get; set; }

        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        public long Duration { get; set; }

        public string CreatedWith { get; set; }

        public ISet<string> Tags { get { return null; } }

        public bool DurationOnly { get; set; }
        // Relations
        public Expression<Func<Workspace, bool>> Workspace {
            get { return (m) => m.Id == WorkspaceId; }
        }

        public Expression<Func<Project, bool>> Project {
            get { return (m) => m.Id == ProjectId; }
        }

        public Expression<Func<Task, bool>> Task {
            get { return (m) => m.Id == TaskId; }
        }
    }
}
