using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
{
    public class ProjectModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<ProjectModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int workspaceRelationId;
        private readonly int clientRelationId;

        public ProjectModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
            clientRelationId = ForeignRelation<ClientModel> (PropertyClientId, PropertyClient);
        }

        protected override void Validate (ValidationContext ctx)
        {
            base.Validate (ctx);

            if (ctx.HasChanged (PropertyName)) {
                if (String.IsNullOrWhiteSpace (Name)) {
                    ctx.AddError (PropertyName, "Project name cannot be empty.");
                } else if (Model.Query<ProjectModel> (
                               (m) => m.Name == Name
                               && m.WorkspaceId == WorkspaceId
                               && m.Id != Id
                           ).NotDeleted ().Count () > 0) {
                    ctx.AddError (PropertyName, "Project with such name already exists.");
                }
            }

            if (ctx.HasChanged (PropertyWorkspaceId)
                || ctx.HasChanged (PropertyIsBillable)
                || ctx.HasChanged (PropertyIsTemplate)) {

                ctx.ClearErrors (PropertyWorkspaceId);
                ctx.ClearErrors (PropertyWorkspace);

                if (WorkspaceId == null) {
                    ctx.AddError (PropertyWorkspaceId, "Project must be associated with a workspace.");
                } else if (Workspace == null) {
                    ctx.AddError (PropertyWorkspace, "Associated workspace could not be found.");
                }

                // Check premium feature usage
                if (IsBillable && Workspace != null && !Workspace.IsPremium) {
                    ctx.AddError (PropertyIsBillable, "Billable projects can only exist in premium workspaces.");
                } else {
                    ctx.ClearErrors (PropertyIsBillable);
                }

                if (IsTemplate && Workspace != null && !Workspace.IsPremium) {
                    ctx.AddError (PropertyIsTemplate, "Projects can be templates only in premium workspaces.");
                } else {
                    ctx.ClearErrors (PropertyIsTemplate);
                }

                if (UseTasksEstimate && Workspace != null && !Workspace.IsPremium) {
                    ctx.AddError (PropertyUseTasksEstimate, "Task estimates are only available in premium workspaces.");
                } else {
                    ctx.ClearErrors (PropertyUseTasksEstimate);
                }
            }
        }

        #region Data

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

        private bool priv;
        public static readonly string PropertyIsPrivate = GetPropertyName ((m) => m.IsPrivate);

        [JsonProperty ("is_private")]
        public bool IsPrivate {
            get { return priv; }
            set {
                if (priv == value)
                    return;

                ChangePropertyAndNotify (PropertyIsPrivate, delegate {
                    priv = value;
                });
            }
        }

        private bool taskEstimate;
        public static readonly string PropertyUseTasksEstimate = GetPropertyName ((m) => m.UseTasksEstimate);

        [JsonProperty ("auto_estimates")]
        public bool UseTasksEstimate {
            get { return taskEstimate; }
            set {
                if (taskEstimate == value)
                    return;

                ChangePropertyAndNotify (PropertyUseTasksEstimate, delegate {
                    taskEstimate = value;
                });
            }
        }

        private bool billable;
        public static readonly string PropertyIsBillable = GetPropertyName ((m) => m.IsBillable);

        [JsonProperty ("billable")]
        public bool IsBillable {
            get { return billable; }
            set {
                if (billable == value)
                    return;

                ChangePropertyAndNotify (PropertyIsBillable, delegate {
                    billable = value;
                });
            }
        }

        private string color;
        public static readonly string PropertyColor = GetPropertyName ((m) => m.Color);

        [JsonProperty ("color")]
        public string Color {
            get { return color; }
            set {
                if (color == value)
                    return;

                ChangePropertyAndNotify (PropertyColor, delegate {
                    color = value;
                });
            }
        }

        private bool template;
        public static readonly string PropertyIsTemplate = GetPropertyName ((m) => m.IsTemplate);

        [JsonProperty ("template")]
        public bool IsTemplate {
            get { return template; }
            set {
                if (template == value)
                    return;

                ChangePropertyAndNotify (PropertyIsTemplate, delegate {
                    template = value;
                });
            }
        }

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

        #endregion

        #region Relations

        public static readonly string PropertyWorkspaceId = GetPropertyName ((m) => m.WorkspaceId);

        public Guid? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        public static readonly string PropertyWorkspace = GetPropertyName ((m) => m.Workspace);

        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public static readonly string PropertyClientId = GetPropertyName ((m) => m.ClientId);

        public Guid? ClientId {
            get { return GetForeignId (clientRelationId); }
            set { SetForeignId (clientRelationId, value); }
        }

        public static readonly string PropertyClient = GetPropertyName ((m) => m.Client);

        [SQLite.Ignore]
        [JsonProperty ("cid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
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
