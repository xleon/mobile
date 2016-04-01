using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Models
{
    public class TimeEntryInfo
    {
        public IWorkspaceData WorkspaceData { get; private set; }
        public IProjectData ProjectData { get; private set; }
        public IClientData ClientData { get; private set; }
        public ITaskData TaskData { get; private set; }
        public IReadOnlyList<ITagData> Tags { get; private set; }
        public int Color { get; private set; }

        public TimeEntryInfo (
            IWorkspaceData wsData,
            IProjectData projectData,
            IClientData clientData,
            ITaskData taskData,
            IReadOnlyList<ITagData> tags,
            int color)
        {
            WorkspaceData = wsData;
            ProjectData = projectData;
            ClientData = clientData;
            TaskData = taskData;
            Tags = tags;
            Color = color;
        }

        public TimeEntryInfo With (
            IWorkspaceData wsData = null,
            IProjectData projectData = null,
            IClientData clientData = null,
            ITaskData taskData = null,
            IReadOnlyList<ITagData> tags = null,
            int? color = null)
        {
            return new TimeEntryInfo (
                       wsData ?? this.WorkspaceData,
                       projectData ?? this.ProjectData,
                       clientData ?? this.ClientData,
                       taskData ?? this.TaskData,
                       tags ?? this.Tags,
                       color.HasValue ? color.Value : this.Color);
        }
    }
}

