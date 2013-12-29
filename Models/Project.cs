using System;
using System.Linq.Expressions;

namespace TogglDoodle.Models
{
    public class Project : Model
    {
        public long? WorkspaceId { get; set; }

        public Expression<Func<Workspace, bool>> Workspace {
            get { return (m) => m.Id == WorkspaceId; }
        }
    }
}
