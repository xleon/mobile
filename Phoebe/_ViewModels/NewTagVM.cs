using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    public class NewTagVM : IDisposable
    {
        private readonly WorkspaceData workspace;
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

        public TagData SaveTag (string tagName, SyncTestOptions testOptions = null)
        {
            var existing =
                appState.Tags.Values.SingleOrDefault (
                    r => r.WorkspaceId == workspace.Id && r.Name == tagName);

            var tag = existing
            ?? new TagData {
                Id = Guid.NewGuid (),
                Name = tagName,
                WorkspaceId = workspace.Id,
                WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0
            };

            RxChain.Send (new DataMsg.TagsPut (new[] { tag }), testOptions);
            return tag;
        }
    }
}
