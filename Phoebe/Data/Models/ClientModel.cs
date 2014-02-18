using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class ClientModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<ClientModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int workspaceRelationId;

        public ClientModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
        }

        protected override void Validate (ValidationContext ctx)
        {
            base.Validate (ctx);

            if (ctx.HasChanged (PropertyName)) {
                if (String.IsNullOrWhiteSpace (Name)) {
                    ctx.AddError (PropertyName, "Client name cannot be empty.");
                } else if (Model.Query<ClientModel> (
                               (m) => m.Name == Name
                               && m.WorkspaceId == WorkspaceId
                               && m.Id != Id
                           ).NotDeleted ().Count () > 0) {
                    ctx.AddError (PropertyName, "Client with such name already exists.");
                }
            }

            if (ctx.HasChanged (PropertyWorkspaceId)) {
                ctx.ClearErrors (PropertyWorkspaceId);
                ctx.ClearErrors (PropertyWorkspace);

                if (WorkspaceId == null) {
                    ctx.AddError (PropertyWorkspaceId, "Client must be associated with a workspace.");
                } else if (Workspace == null) {
                    ctx.AddError (PropertyWorkspace, "Associated workspace could not be found.");
                }
            }
        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("name")]
        public string Name {
            get {
                lock (SyncRoot) {
                    return name;
                }
            }
            set {
                lock (SyncRoot) {
                    if (name == value)
                        return;

                    ChangePropertyAndNotify (PropertyName, delegate {
                        name = value;
                    });
                }
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

        public IModelQuery<ProjectModel> Projects {
            get { return Model.Query<ProjectModel> ((m) => m.ClientId == Id); }
        }

        #endregion
    }
}
