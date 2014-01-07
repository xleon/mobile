using System;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Data
{
    public class ProjectModel : Model
    {
        public static long NextId {
            get { return Model.NextId<ProjectModel> (); }
        }

        private readonly int workspaceRelationId;
        private readonly int clientRelationId;

        public ProjectModel ()
        {
            workspaceRelationId = ForeignRelation (() => WorkspaceId, () => Workspace);
            clientRelationId = ForeignRelation (() => ClientId, () => Client);
        }

        #region Data

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

        private bool priv;

        public bool IsPrivate {
            get { return priv; }
            set {
                if (priv == value)
                    return;

                ChangePropertyAndNotify (() => IsPrivate, delegate {
                    priv = value;
                });
            }
        }

        private bool taskEstimate;

        public bool UseTasksEstimate {
            get { return taskEstimate; }
            set {
                if (taskEstimate == value)
                    return;

                ChangePropertyAndNotify (() => UseTasksEstimate, delegate {
                    taskEstimate = value;
                });
            }
        }

        private bool billable;

        public bool IsBillable {
            get { return billable; }
            set {
                if (billable == value)
                    return;

                ChangePropertyAndNotify (() => IsBillable, delegate {
                    billable = value;
                });
            }
        }

        private string color;

        public string Color {
            get { return color; }
            set {
                if (color == value)
                    return;

                ChangePropertyAndNotify (() => Color, delegate {
                    color = value;
                });
            }
        }

        private bool template;

        public bool IsTemplate {
            get { return template; }
            set {
                if (template == value)
                    return;

                ChangePropertyAndNotify (() => IsTemplate, delegate {
                    template = value;
                });
            }
        }

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

        #endregion

        #region Relations

        public long? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        [SQLite.Ignore]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public long? ClientId {
            get { return GetForeignId (clientRelationId); }
            set { SetForeignId (clientRelationId, value); }
        }

        [SQLite.Ignore]
        public ClientModel Client {
            get { return GetForeignModel<ClientModel> (clientRelationId); }
            set { SetForeignModel (clientRelationId, value); }
        }

        public IModelQuery<TaskModel> Tasks {
            get { return Model.Query<TaskModel> ((m) => m.ProjectId == Id); }
        }

        public IModelQuery<TimeEntryModel> TimeEntries {
            get { return Model.Query<TimeEntryModel> ((m) => m.ProjectId == Id); }
        }

        #endregion

    }
}
