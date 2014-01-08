using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
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

        [JsonProperty ("name")]
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

        [JsonProperty ("active")]
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

        [JsonProperty ("estimated_seconds")]
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

        [JsonProperty ("wid")]
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

        [JsonProperty ("pid")]
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

        public IModelQuery<TimeEntryModel> TimeEntries {
            get { return Model.Query<TimeEntryModel> ((m) => m.TaskId == Id); }
        }

        #endregion

    }
}
