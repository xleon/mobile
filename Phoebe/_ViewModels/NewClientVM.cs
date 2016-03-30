using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using XPlatUtils;
using Toggl.Phoebe._Data;

namespace Toggl.Phoebe._ViewModels
{
    public class NewClientVM : IDisposable
    {
        private IClientData model;
        private readonly AppState appState;
        private readonly IWorkspaceData workspace;

        public NewClientVM (AppState appState, Guid workspaceId)
        {
            workspace = appState.Workspaces[workspaceId];
            this.appState = appState;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Client Screen";
        }

        public void Dispose ()
        {
        }

        public IClientData SaveClient (string clientName, SyncTestOptions testOptions = null)
        {
            model = new ClientData {
                SyncState = SyncState.CreatePending,
                Id = Guid.NewGuid (),
                WorkspaceId = workspace.Id,
                Name = clientName,
                WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0
            };

            // Save client name to make sure it doesn't change while iterating
            var existing =
                appState.Clients.Values
                .SingleOrDefault (
                    r => r.WorkspaceId == model.WorkspaceId && r.Name == clientName);
            model = existing ?? model;
            RxChain.Send (new DataMsg.ClientDataPut (model), testOptions);
            return model;
        }
    }
}
