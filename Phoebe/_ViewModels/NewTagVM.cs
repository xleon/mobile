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
        private readonly TimerState timerState;

        public NewTagVM (TimerState timerState, Guid workspaceId)
        {
            this.timerState = timerState;
            this.workspace = timerState.Workspaces[workspaceId];
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Tag Screen";
        }

        public string TagName { get; set; }

        public void Dispose ()
        {
        }

        public TagData SaveTag ()
        {
            var existing =
                timerState.Tags.Values.SingleOrDefault (r => r.WorkspaceId == workspace.Id && r.Name == TagName);

            var tag = existing
                ?? new TagData {
                    Id = Guid.NewGuid (),
                    Name = TagName,
                    WorkspaceId = workspace.Id,
                    WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0
                };

            RxChain.Send (new DataMsg.TagPut (tag));
            return tag;
        }
    }
}
