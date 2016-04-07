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
    public class NewTagVM : IDisposable
    {
        private readonly IWorkspaceData workspace;
        private readonly AppState appState;

        public NewTagVM(AppState appState, Guid workspaceId)
        {
            this.appState = appState;
            workspace = appState.Workspaces[workspaceId];
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Tag Screen";
        }

        public void Dispose()
        {
        }

        public Task<ITagData> SaveTagAsync (string tagName, SyncTestOptions testOptions = null)
        {
            var tcs = new TaskCompletionSource<ITagData> ();
            var existing =
                appState.Tags.Values.SingleOrDefault(
                    r => r.WorkspaceId == workspace.Id && r.Name == tagName);

            if (existing != null) {
                return Task.FromResult (existing);
            }

            var tag = TagData.Create (x => {
                x.Name = tagName;
                x.WorkspaceId = workspace.Id;
                x.WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0;
            });

            RxChain.Send (new DataMsg.TagsPut (new[] {tag}), new SyncTestOptions (false, (state, sent, queued) => {
                var tagData = state.Tags.Values.First (x => x.WorkspaceId == tag.WorkspaceId && x.Name == tag.Name);
                tcs.SetResult (tagData);
            }));

            return tcs.Task;
        }
    }
}
