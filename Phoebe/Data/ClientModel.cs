using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
{
    public class ClientModel : Model
    {
        private readonly int workspaceRelationId;

        public ClientModel ()
        {
            workspaceRelationId = ForeignRelation (() => WorkspaceId, () => Workspace);
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

        #endregion

        #region Relations

        [JsonProperty ("wid")]
        public Guid? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        [DontDirty]
        [SQLite.Ignore]
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
