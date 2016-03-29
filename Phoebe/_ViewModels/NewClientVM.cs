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

        public NewClientVM (AppState appState, Guid workspaceId)
        {
            var workspace = appState.Workspaces[workspaceId];
            this.appState = appState;
            model = new ClientData {
                SyncState = SyncState.CreatePending,
                Id = Guid.NewGuid (),
                WorkspaceId = workspaceId,
                WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0
            };
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Client Screen";
        }

        public void Dispose ()
        {
        }

        public IClientData SaveClient (string clientName, SyncTestOptions testOptions = null)
        {
            // Save client name to make sure it doesn't change while iterating
            var existing =
                appState.Clients.Values
                .SingleOrDefault (
                    r => r.WorkspaceId == model.WorkspaceId && r.Name == clientName);

            model = existing ?? model.With (x => x.Name = clientName);

            RxChain.Send (new DataMsg.ClientDataPut (model), testOptions);

            return model;
        }
    }
}
