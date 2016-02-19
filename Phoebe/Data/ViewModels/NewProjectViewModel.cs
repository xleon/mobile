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
            ServiceContainer.Resolve<ITracker>().CurrentScreen = "New Project";
        }

        public static async Task<NewProjectViewModel> Init (Guid workspaceId)
        {
            var workspaceModel = new WorkspaceModel (workspaceId);
            await workspaceModel.LoadAsync();

            return new NewProjectViewModel (new ProjectModel {
                Workspace = workspaceModel,
                IsActive = true,
                IsPrivate = true
            });
        }

        public void Dispose()
        {
        }

        public string ClientName { get; set; }

        public void SetClient (ClientData clientData)
        {
            model.Client = new ClientModel (clientData);
            ClientName = clientData.Name;
        }

        public async Task<ProjectData> SaveProject (string projectName, int projectColor)
        {
            model.Name = projectName;
            model.Color = projectColor;

            // Save new project.
            await model.SaveAsync();

            // Create an extra model for Project / User relationship
            var userData = ServiceContainer.Resolve<AuthManager>().User;

            var projectUserModel = new ProjectUserModel();
            projectUserModel.Project = model;
            projectUserModel.User = new UserModel (userData);

            // Save relationship.
            await projectUserModel.SaveAsync();

            return model.Data;
        }

        public async Task<bool> ExistProjectWithName (string projectName)
        {
            var dataStore = ServiceContainer.Resolve<IDataStore>();
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

