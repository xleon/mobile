using System;
using System.Linq.Expressions;

namespace TogglDoodle.Models
{
    public class ProjectModel : Model
    {
        public long? WorkspaceId { get; set; }

        public Expression<Func<WorkspaceModel, bool>> Workspace {
            get { return (m) => m.Id == WorkspaceId; }
        }
    }
}
