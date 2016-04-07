using System;
using System.Linq;
using System.Reactive.Linq;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    [ImplementPropertyChanged]
    public class NewProjectVM : IDisposable
    {
        private IProjectData model;
        private readonly AppState appState;
        private readonly IWorkspaceData workspace;

        public NewProjectVM(AppState appState, Guid workspaceId)
        {
            this.appState = appState;
            workspace = appState.Workspaces[workspaceId];
            model = ProjectData.Create(x =>
            {
                x.WorkspaceId = workspaceId;
                x.WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0;
                x.IsActive = true;
                x.IsPrivate = true;
            });
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Project";
        }

        public void Dispose()
        {
        }

        public string ClientName { get; set; }

        public void SetClient(IClientData clientData)
        {
            model = model.With(x =>
            {
                x.ClientId = clientData.Id;
                x.ClientRemoteId = clientData.RemoteId;
            });
            ClientName = clientData.Name;
        }

        public IProjectData SaveProject(string projectName, int projectColor, RxChain.Continuation cont = null)
        {
            model = model.With(x =>
            {
                x.Name = projectName;
                x.Color = projectColor;
            });

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
            RxChain.Send(new DataMsg.ProjectDataPut(model), cont);

            return model;
        }

        public bool ExistProjectWithName(string projectName)
        {
            Guid clientId = model.ClientId;
            return appState.Projects.Values.Any(r => r.Name == projectName && r.ClientId == clientId);
        }

        public bool ContainsClients(Guid workspaceId)
        {
            return appState.Clients.Values.Any(r => r.DeletedAt == null && r.WorkspaceId == workspaceId);
        }
    }
}
