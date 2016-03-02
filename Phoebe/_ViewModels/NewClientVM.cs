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
        private ClientData model;
        private readonly TimerState timerState;

        public NewClientVM (TimerState timerState, Guid workspaceId)
        {
			var workspace = timerState.Workspaces[workspaceId];
            this.timerState = timerState;
            model = new ClientData {
                Id = Guid.NewGuid (),
                WorkspaceId = workspaceId,
                WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0
            };
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Client Screen";
        }

        public void Dispose ()
        {
        }

        public string ClientName { get; set; }

        public ClientData SaveClient (SyncTestOptions testOptions = null)
        {
            // Save client name to make sure it doesn't change while iterating
            var clientName = ClientName;
            var existing =
                timerState.Clients.Values
                          .SingleOrDefault (
                              r => r.WorkspaceId == model.WorkspaceId && r.Name == clientName);

            if (existing != null) {
                model = existing;
            } else {
                model.Name = clientName;
            }

            RxChain.Send (new DataMsg.ClientDataPut (model), testOptions);
            
            return model;
        }
    }
}
