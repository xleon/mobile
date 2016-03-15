using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XPlatUtils;

namespace Toggl.Phoebe._Data.Models
{
    public class TimeEntryInfo
    {
        public WorkspaceData WorkspaceData { get; private set; }
        public ProjectData ProjectData { get; private set; }
        public ClientData ClientData { get; private set; }
        public TaskData TaskData { get; private set; }
        public IReadOnlyList<TagData> Tags { get; private set; }
        public int Color { get; private set; }

        public TimeEntryInfo (
            WorkspaceData wsData,
            ProjectData projectData,
            ClientData clientData,
            TaskData taskData,
            IReadOnlyList<TagData> tags,
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
            WorkspaceData wsData = null,
            ProjectData projectData = null,
            ClientData clientData = null,
            TaskData taskData = null,
            IReadOnlyList<TagData> tags = null,
            int? color = null)
        {
            return new TimeEntryInfo (
                       wsData ?? this.WorkspaceData,
                       projectData ?? this.ProjectData,
                       clientData ?? this.ClientData,
                       taskData ?? this.TaskData,
                       tags ?? this.Tags,
                       color.HasValue ? color.Value : this.Color
                   );
        }
    }
}

