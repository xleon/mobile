using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Net
{
    public class UserRelatedModels
    {
        public UserModel User { get; set; }

        public IEnumerable<WorkspaceModel> Workspaces { get; set; }

        public IEnumerable<TagModel> Tags { get; set; }

        public IEnumerable<ProjectModel> Projects { get; set; }

        public IEnumerable<TaskModel> Tasks { get; set; }

        public IEnumerable<ClientModel> Clients { get; set; }

        public IEnumerable<TimeEntryModel> TimeEntries { get; set; }

        public DateTime Timestamp;
    }
}
