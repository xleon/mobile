using System;

namespace Toggl.Phoebe.Models
{
    public class TaskModel : Model
    {
        public static long NextId {
            get { return Model.NextId<TaskModel> (); }
        }

        private readonly int workspaceRelationId;
        private readonly int projectRelationId;

        public TaskModel ()
        {
            workspaceRelationId = ForeignRelation (() => WorkspaceId, () => Workspace);
            projectRelationId = ForeignRelation (() => ProjectId, () => Project);
        }

        #region Data

        private string name;

        public string Name {
            get { return name; }
            set {
                if (name == value)
                    return;

                ChangePropertyAndNotify (() => Name, delegate {
                    name = value;
                });
            }
        }

        private bool active;

        public bool IsActive {
            get { return active; }
            set {
                if (active == value)
                    return;

                ChangePropertyAndNotify (() => IsActive, delegate {
                    active = value;
                });
            }
        }

        private long estimate;

        public long Estimate {
            get { return estimate; }
            set {
                if (estimate == value)
                    return;

                ChangePropertyAndNotify (() => Estimate, delegate {
                    estimate = value;
                });
            }
        }

        #endregion

        #region Relations

        public long? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        [DontDirty]
        [SQLite.Ignore]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public long? ProjectId {
            get { return GetForeignId (projectRelationId); }
            set { SetForeignId (projectRelationId, value); }
        }

        [DontDirty]
        [SQLite.Ignore]
        public ProjectModel Project {
            get { return GetForeignModel<ProjectModel> (projectRelationId); }
            set { SetForeignModel (projectRelationId, value); }
        }
        // TODO: Reverse relation for tasks

        #endregion

    }
}
