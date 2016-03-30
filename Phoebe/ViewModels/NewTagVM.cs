using System;
using System.Linq;
using System.Reactive.Linq;
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

        public NewTagVM (AppState appState, Guid workspaceId)
        {
            this.appState = appState;
            workspace = appState.Workspaces[workspaceId];
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Tag Screen";
        }

        public void Dispose ()
        {
        }

        public ITagData SaveTag (string tagName, SyncTestOptions testOptions = null)
        {
            var existing =
                appState.Tags.Values.SingleOrDefault (
                    r => r.WorkspaceId == workspace.Id && r.Name == tagName);

            var tag = existing
            ?? new TagData {
                Id = Guid.NewGuid (),
                Name = tagName,
                WorkspaceId = workspace.Id,
                WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0,
                SyncState = SyncState.CreatePending
            };

            RxChain.Send (new DataMsg.TagsPut (new[] { tag }), testOptions);
            return tag;
        }
    }
}
