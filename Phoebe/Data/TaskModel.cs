using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
{
    public class TaskModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<TaskModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int workspaceRelationId;
        private readonly int projectRelationId;

        public TaskModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
            projectRelationId = ForeignRelation<ProjectModel> (PropertyProjectId, PropertyProject);
        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("name")]
        public string Name {
            get { return name; }
            set {
                if (name == value)
                    return;

                ChangePropertyAndNotify (PropertyName, delegate {
                    name = value;
                });
            }
        }

        private bool active;
        public static readonly string PropertyIsActive = GetPropertyName ((m) => m.IsActive);

        [JsonProperty ("active")]
        public bool IsActive {
            get { return active; }
            set {
                if (active == value)
                    return;

                ChangePropertyAndNotify (PropertyIsActive, delegate {
                    active = value;
                });
            }
        }

        private long estimate;
        public static readonly string PropertyEstimate = GetPropertyName ((m) => m.Estimate);

        [JsonProperty ("estimated_seconds")]
        public long Estimate {
            get { return estimate; }
            set {
                if (estimate == value)
                    return;

                ChangePropertyAndNotify (PropertyEstimate, delegate {
                    estimate = value;
                });
            }
        }

        #endregion

        #region Relations

        public static readonly string PropertyWorkspaceId = GetPropertyName ((m) => m.WorkspaceId);

        public Guid? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        public static readonly string PropertyWorkspace = GetPropertyName ((m) => m.Workspace);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public static readonly string PropertyProjectId = GetPropertyName ((m) => m.ProjectId);

        public Guid? ProjectId {
            get { return GetForeignId (projectRelationId); }
            set { SetForeignId (projectRelationId, value); }
        }

        public static readonly string PropertyProject = GetPropertyName ((m) => m.Project);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("pid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
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
