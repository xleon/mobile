using System;
using System.Threading.Tasks;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class NewProjectViewModel : IDisposable
    {
        private ProjectModel model;

        public NewProjectViewModel (ProjectModel model)
        {
            this.model = model;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Project";
        }

        public static async Task<NewProjectViewModel> Init (Guid workspaceId)
        {
            var workspaceModel = new WorkspaceModel (workspaceId);
            await workspaceModel.LoadAsync ();

            return new NewProjectViewModel (new ProjectModel {
                Workspace = workspaceModel,
                IsActive = true,
                IsPrivate = true
            });
        }

        public void Dispose ()
        {
            model = null;
        }

        public string ProjectName { get; set; }

        public int ProjectColor { get; set; }

        public string ClientName { get; set; }

        public Guid ProjectId {  get { return model.Id; } } // TODO: not good :(

        public void SetClient (ClientData clientData)
        {
            model.Client = new ClientModel (clientData);
            ClientName = clientData.Name;
        }

        public async Task<SaveProjectResult> SaveProjectModel ()
        {
            // Project name is empty
            if (string.IsNullOrEmpty (ProjectName)) {
                return SaveProjectResult.NameIsEmpty;
            }

            // Project name is used
            var exists = await ExistProjectWithName (ProjectName);
            if (exists) {
                return SaveProjectResult.NameExists;
            }

            model.Name = ProjectName;
            model.Color = ProjectColor;

            // Save new project.
            await model.SaveAsync();

            // Create an extra model for Project / User relationship
            var userData = ServiceContainer.Resolve<AuthManager> ().User;

            var projectUserModel = new ProjectUserModel ();
            projectUserModel.Project = model;
            projectUserModel.User = new UserModel (userData);

            // Save relationship.
            await projectUserModel.SaveAsync ();

            return SaveProjectResult.SaveOk;
        }

        private async Task<bool> ExistProjectWithName (string projectName)
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            Guid clientId = (model.Client == null) ? Guid.Empty : model.Client.Id;
            var existWithName = await dataStore.Table<ProjectData>().ExistWithNameAsync (projectName, clientId);
            return existWithName;
        }

        public enum SaveProjectResult {
            SaveOk = 0,
            NameIsEmpty = 1,
            NameExists = 2
        }
    }
}

