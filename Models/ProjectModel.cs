using System;
using System.Linq.Expressions;

namespace TogglDoodle.Models
{
    public class ProjectModel : Model
    {
        public static long NextId {
            get { return Model.NextId<ProjectModel> (); }
        }

        private readonly int workspaceRelationId;

        public ProjectModel ()
        {
            workspaceRelationId = ForeignRelation (() => WorkspaceId, () => Workspace);
        }

        public long? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        [SQLite.Ignore]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }
    }
}
