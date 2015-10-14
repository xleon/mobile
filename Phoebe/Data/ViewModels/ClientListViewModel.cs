using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class ClientListViewModel : IViewModel<ProjectModel>
    {
        private bool isLoading;
        private WorkspaceClientsView clientList;
        private ProjectModel model;
        private Guid workspaceId;

        public ClientListViewModel (Guid workspaceId, ProjectModel projectModel)
        {
            this.model = projectModel;
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Client";
        }

        public async Task Init ()
        {
            IsLoading = true;

            clientList = new WorkspaceClientsView (workspaceId);

            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                model = null;
            }

            IsLoading = false;
        }

        public void Dispose ()
        {
            model.PropertyChanged -= OnModelPropertyChanged;
            clientList.Dispose ();
            model = null;
        }


        public Guid WorkspaceId
        {
            get {
                return workspaceId;
            }
        }

        public IDataView<ClientData> ClientListDataView
        {
            get {
                return clientList;
            }
        }

        public event EventHandler OnModelChanged;

        public ProjectModel Model
        {
            get {
                return model;
            }

            private set {

                model = value;

                if (OnModelChanged != null) {
                    OnModelChanged (this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler OnIsLoadingChanged;

        public bool IsLoading
        {
            get {
                return isLoading;
            }
            private set {
                isLoading = value;

                if (OnIsLoadingChanged != null) {
                    OnIsLoadingChanged (this, EventArgs.Empty);
                }
            }
        }

        private void OnModelPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == TimeEntryModel.PropertyWorkspace) {
                if (clientList != null) {
                    clientList.WorkspaceId = model.Workspace.Id;
                    workspaceId = model.Workspace.Id;
                }
            }
        }
    }
}
