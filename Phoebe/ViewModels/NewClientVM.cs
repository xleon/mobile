using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    public class NewClientVM : IDisposable
    {
        private IClientData model;
        private readonly AppState appState;
        private readonly IWorkspaceData workspace;

        public NewClientVM(AppState appState, Guid workspaceId)
        {
            workspace = appState.Workspaces[workspaceId];
            this.appState = appState;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Client Screen";
        }

        public void Dispose()
        {
        }

        public Task<IClientData> SaveClientAsync(string clientName, RxChain.Continuation continuationOptions = null)
        {
            var tcs = new TaskCompletionSource<IClientData> ();
            model = ClientData.Create(x =>
            {
                x.Name = clientName;
                x.WorkspaceId = workspace.Id;
                x.WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0;
            });

            // Save client name to make sure it doesn't change while iterating
            var existing =
                appState.Clients.Values
                .SingleOrDefault(
                    r => r.WorkspaceId == model.WorkspaceId && r.Name == clientName);
            if (existing != null)
            {
                return Task.FromResult(existing);
            }

            RxChain.Send(new DataMsg.ClientDataPut(model), new RxChain.Continuation((state) =>
            {
                var clientData = state.Clients.Values.First(x => x.WorkspaceId == model.WorkspaceId && x.Name == model.Name);
                tcs.SetResult(clientData);
            }));

            return tcs.Task;
        }
    }
}
