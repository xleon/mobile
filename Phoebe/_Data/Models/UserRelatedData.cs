using System;
using System.Collections.Generic;

namespace Toggl.Phoebe._Data.Models
{
    public class UserRelatedData
    {
        public UserData User { get; set; }

        public IEnumerable<WorkspaceData> Workspaces { get; set; }

        public IEnumerable<TagData> Tags { get; set; }

        public IEnumerable<ProjectData> Projects { get; set; }

        public IEnumerable<TaskData> Tasks { get; set; }

        public IEnumerable<ClientData> Clients { get; set; }

        public IEnumerable<TimeEntryData> TimeEntries { get; set; }

        public DateTime Timestamp;
    }
}
