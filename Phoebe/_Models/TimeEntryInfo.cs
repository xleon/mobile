using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Models
{
    public class TimeEntryInfo
    {
        public ProjectData ProjectData { get; set; }
        public ClientData ClientData { get; set; }
        public TaskData TaskData { get; set; }
        public string Description { get; set; }
        public int Color { get; set; }
        public bool IsBillable { get; set; }
        public int NumberOfTags { get; set; }
    }
}

