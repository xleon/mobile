using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    [Table ("ProjectModel")]
    public class ProjectData : CommonData
    {
        public ProjectData ()
        {
        }

        public ProjectData (ProjectData other) : base (other)
        {
            Name = other.Name;
            Color = other.Color;
            IsActive = other.IsActive;
            IsBillable = other.IsBillable;
            IsPrivate = other.IsPrivate;
            IsTemplate = other.IsTemplate;
            UseTasksEstimate = other.UseTasksEstimate;
            WorkspaceId = other.WorkspaceId;
            ClientId = other.ClientId;
        }

        public string Name { get; set; }

        public int Color { get; set; }

        public bool IsActive { get; set; }

        public bool IsBillable { get; set; }

        public bool IsPrivate { get; set; }

        public bool IsTemplate { get; set; }

        public bool UseTasksEstimate { get; set; }

        public Guid WorkspaceId { get; set; }

        public Guid? ClientId { get; set; }
    }
}
