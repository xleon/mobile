using System;

namespace Toggl.Phoebe.Models
{
    public class ClientModel : Model
    {
        public static long NextId {
            get { return Model.NextId<ClientModel> (); }
        }

        private readonly int workspaceRelationId;

        public ClientModel ()
        {
            workspaceRelationId = ForeignRelation (() => WorkspaceId, () => Workspace);
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
        // TODO: Reverse relation for tasks

        #endregion

    }
}
