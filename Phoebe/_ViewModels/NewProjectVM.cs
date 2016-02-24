using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using XPlatUtils;
using Toggl.Phoebe._Data;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class NewProjectVM : IDisposable
    {
        private TimerState timerState;
        private WorkspaceData workspace;
        private ProjectData model;

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
            model = null;
        }

        public string ProjectName { get; set; }

        public int ProjectColor { get; set; }

        public string ClientName { get; set; }

        public Guid ProjectId {  get { return model.Id; } } // TODO: not good :(

        public void SetClient (ClientData clientData)
        {
            model.ClientId = clientData.Id;
            model.ClientRemoteId = clientData.RemoteId;
            ClientName = clientData.Name;
        }

        public SaveProjectResult SaveProjectData ()
        {
            // Project name is empty
            if (string.IsNullOrEmpty (ProjectName)) {
                return SaveProjectResult.NameIsEmpty;
            }

            // Project name is used
            var exists = ExistProjectWithName (ProjectName);
            if (exists) {
                return SaveProjectResult.NameExists;
            }

            model.Name = ProjectName;
            model.Color = ProjectColor;

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

            // TODO: Wait result from RxChain?
            return SaveProjectResult.SaveOk;
        }

        private bool ExistProjectWithName (string projectName)
        {
            Guid clientId = model.ClientId;
            return timerState.Projects.Values.Any (r => r.Name == projectName && r.ClientId == clientId);;
        }

        public enum SaveProjectResult {
            SaveOk = 0,
            NameIsEmpty = 1,
            NameExists = 2
        }
    }
}
