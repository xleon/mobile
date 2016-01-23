using System;
using System.Collections.Generic;

namespace Toggl.Phoebe._Data.Json
{
    public class UserRelatedJson
    {
        public UserJson User { get; set; }

        public IEnumerable<WorkspaceJson> Workspaces { get; set; }

        public IEnumerable<TagJson> Tags { get; set; }

        public IEnumerable<ProjectJson> Projects { get; set; }

        public IEnumerable<TaskJson> Tasks { get; set; }

        public IEnumerable<ClientJson> Clients { get; set; }

        public IEnumerable<TimeEntryJson> TimeEntries { get; set; }

        public DateTime Timestamp;
    }
}
