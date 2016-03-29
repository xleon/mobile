using System;
using System.Linq;
using System.Reactive.Linq;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class NewProjectVM : IDisposable
    {
        private readonly AppState appState;
        private readonly IWorkspaceData workspace;
        private readonly ProjectData model;

        public NewProjectVM (AppState appState, Guid workspaceId)
        {
            this.appState = appState;
            workspace = appState.Workspaces[workspaceId];
            model = new ProjectData {
                Id = Guid.NewGuid (),
                WorkspaceId = workspaceId,
                WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0,
                IsActive = true,
                IsPrivate = true,
                SyncState = SyncState.CreatePending
            };
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Project";
        }

        public void Dispose ()
        {
        }

        public string ClientName { get; set; }

        public void SetClient (IClientData clientData)
        {
            model.ClientId = clientData.Id;
            model.ClientRemoteId = clientData.RemoteId;
            ClientName = clientData.Name;
        }

        public IProjectData SaveProject (string projectName, int projectColor, SyncTestOptions testOptions = null)
        {
            model.Name = projectName;
            model.Color = projectColor;

            // TODO: RX for the moment, ProjectUserData
            // is not used in any case.
            /*
            var projectUser = new ProjectUserData {
                Id = Guid.NewGuid (),
                ProjectId = model.Id,
                UserId = appState.User.Id,
                ProjectRemoteId = model.RemoteId.HasValue ? model.RemoteId.Value : 0,
                UserRemoteId = appState.User.RemoteId.HasValue ? appState.User.RemoteId.Value : 0,
                SyncPending = true
            };
            */
            // Save new project and relationship
            RxChain.Send (new DataMsg.ProjectDataPut (model), testOptions);

            return model;
        }

        public bool ExistProjectWithName (string projectName)
        {
            Guid clientId = model.ClientId;
            return appState.Projects.Values.Any (r => r.Name == projectName && r.ClientId == clientId);
        }

        public bool ContainsClients (Guid workspaceId)
        {
            return appState.Clients.Values.Any (r => r.DeletedAt == null && r.WorkspaceId == workspaceId);
        }
    }
}
