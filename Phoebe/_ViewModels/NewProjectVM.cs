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
        private readonly TimerState timerState;
        private readonly WorkspaceData workspace;
        private readonly ProjectData model;

        public NewProjectVM (TimerState timerState, Guid workspaceId)
        {
            this.timerState = timerState;
            workspace = timerState.Workspaces[workspaceId];
            model = new ProjectData {
                Id = Guid.NewGuid (),
                WorkspaceId = workspaceId,
                WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0,
                IsActive = true,
                IsPrivate = true
            };
			ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Project";
        }

        public void Dispose ()
        {
        }

        public string ClientName { get; set; }

        public void SetClient (ClientData clientData)
        {
            model.ClientId = clientData.Id;
            model.ClientRemoteId = clientData.RemoteId;
            ClientName = clientData.Name;
        }

        public ProjectData SaveProject (string projectName, int projectColor)
        {
            model.Name = projectName;
            model.Color = projectColor;

            // Create an extra model for Project / User relationship
            var userData = ServiceContainer.Resolve<Net.AuthManager> ().User;

            var projectUser = new ProjectUserData {
                Id = Guid.NewGuid (),
                ProjectId = model.Id,
				UserId = userData.Id,
                ProjectRemoteId = model.RemoteId.HasValue ? model.RemoteId.Value : 0,
                UserRemoteId = userData.RemoteId.HasValue ? userData.RemoteId.Value : 0
            };

            // Save new project and relationship
            RxChain.Send (new DataMsg.ProjectDataPut (model, projectUser));

            return model;
        }

        private bool ExistProjectWithName (string projectName)
        {
            Guid clientId = model.ClientId;
            return timerState.Projects.Values.Any (r => r.Name == projectName && r.ClientId == clientId);
        }
    }
}
