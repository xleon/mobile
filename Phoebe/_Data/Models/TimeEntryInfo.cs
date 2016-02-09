using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XPlatUtils;

namespace Toggl.Phoebe._Data.Models
{
    public class TimeEntryInfo
    {
        public ProjectData ProjectData { get; private set; }
        public ClientData ClientData { get; private set; }
        public TaskData TaskData { get; private set; }
        public string Description { get; private set; }
        public int Color { get; private set; }
        public bool IsBillable { get; private set; }

        public TimeEntryInfo(
            ProjectData projectData,
            ClientData clientData,
            TaskData taskData,
            int color)
        {
            ProjectData = projectData;
            ClientData = clientData;
            TaskData = taskData;
            Color = color;
        }
    }
}

