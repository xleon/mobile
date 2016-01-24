using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    [Table ("ProjectModel")]
    public class ProjectData : CommonData
    {

        public static readonly string[] HexColors = {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

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
