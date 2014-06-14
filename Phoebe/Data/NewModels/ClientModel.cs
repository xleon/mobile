using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data.NewModels
{
    public class ClientModel : Model<ClientData>
    {
        private static string GetPropertyName<T> (Expression<Func<ClientModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<ClientData>.PropertyId;
        public static readonly string PropertyName = GetPropertyName (m => m.Name);
        public static readonly string PropertyWorkspace = GetPropertyName (m => m.Workspace);

        public ClientModel ()
        {
        }

        public ClientModel (ClientData data) : base (data)
        {
        }

        public ClientModel (Guid id) : base (id)
        {
        }

        protected override ClientData Duplicate (ClientData data)
        {
            return new ClientData (data);
        }

        protected override void OnBeforeSave ()
        {
            if (Data.WorkspaceId == Guid.Empty) {
                throw new ValidationException ("Workspace must be set for Client model.");
            }
        }

        protected override void DetectChangedProperties (ClientData oldData, ClientData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.Name != newData.Name)
                OnPropertyChanged (PropertyName);
            if (oldData.WorkspaceId != newData.WorkspaceId || workspace.IsNewInstance)
                OnPropertyChanged (PropertyWorkspace);
        }

        public string Name {
            get {
                EnsureLoaded ();
                return Data.Name;
            }
            set {
                if (Name == value)
                    return;

                MutateData (data => data.Name = value);
            }
        }

        private ForeignRelation<WorkspaceModel> workspace;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            workspace = new ForeignRelation<WorkspaceModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new WorkspaceModel (id),
                Changed = m => MutateData (data => data.WorkspaceId = m.Id),
            };
        }

        [ForeignRelation]
        public WorkspaceModel Workspace {
            get { return workspace.Get (Data.WorkspaceId); }
            set { workspace.Set (value); }
        }

        public static implicit operator ClientModel (ClientData data)
        {
            return new ClientModel (data);
        }

        public static implicit operator ClientData (ClientModel model)
        {
            return model.Data;
        }
    }
}
