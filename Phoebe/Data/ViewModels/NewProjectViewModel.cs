using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class NewProjectViewModel : IViewModel<ProjectModel>
    {
        private bool isLoading;
        private ProjectModel model;
        private WorkspaceModel workspaceModel;
        private Guid workspaceId;


        public NewProjectViewModel (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
        }

        public void Dispose ()
        {
            workspaceModel = null;
            model = null;
        }

        public ProjectModel Model
        {
            get {
                return model;
            }
        }

        public event EventHandler OnIsLoadingChanged;

        public bool IsLoading
        {
            get {
                return isLoading;
            }
            private set {

                if (isLoading  == value) {
                    return;
                }

                isLoading = value;

                if (OnIsLoadingChanged != null) {
                    OnIsLoadingChanged (this, EventArgs.Empty);
                }
            }
        }

        public void Init ()
        {
            IsLoading = true;

            workspaceModel = new WorkspaceModel (workspaceId);
            workspaceModel.LoadAsync ();

            model = new ProjectModel {
                Workspace = workspaceModel,
                IsActive = true,
                IsPrivate = true
            };

            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                model = null;
            }

            IsLoading = false;
        }

        public async Task SaveProjectModel ()
        {
            await model.SaveAsync();

            // Create an extra model for Project / User relationship
            var userData = ServiceContainer.Resolve<AuthManager> ().User;
            var userId = userData != null ? userData.Id : (Guid?)null;

            if (userId.HasValue) {
                var projectUserModel = new ProjectUserModel ();
                projectUserModel.Project = model;
                projectUserModel.User = new UserModel (userId.Value);
                await projectUserModel.SaveAsync ();
            }
        }

        public async Task<bool> ExistProjectWithName (string projectName)
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            Guid clientId = (model.Client == null) ? Guid.Empty : model.Client.Id;
            var existWithName = await dataStore.Table<ProjectData>().ExistWithNameAsync (projectName, clientId);

            return existWithName;
        }
    }
}

