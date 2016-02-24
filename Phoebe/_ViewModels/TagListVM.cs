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
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Phoebe._ViewModels
{
    public class TagListViewModel : IDisposable
    {
        // This viewMode is apparently simple but
        // it needs the code related with the update of
        // the list. (subscription and reload of data
        // everytime a tag is changed/created/deleted

        private readonly Guid workspaceId;
        private readonly List<Guid> previousSelectedIds;

        TagListViewModel (TimerState timerState, Guid workspaceId, List<Guid> previousSelectedIds)
        {
            this.previousSelectedIds = previousSelectedIds;
            this.workspaceId = workspaceId;
            LoadTags (timerState);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Tags";
        }

        public void Dispose ()
        {
            TagCollection = null;
        }

        public ObservableRangeCollection<TagData> TagCollection { get; set; }

        private void LoadTags (TimerState timerState)
        {
            TagCollection = new ObservableRangeCollection<TagData> ();

            var workspaceTags =
                timerState.Tags.Values.Where (r => r.WorkspaceId == workspaceId).ToList ();

            var currentSelectedTags =
                timerState.Tags.Values.Where (r => previousSelectedIds.Contains (r.Id)).ToList ();

            // TODO:
            // There is an strange case, tags are created again across
            // workspaces. To avoid display similar names
            // on the list the diff and corrupt data, the filter is done by
            // names. The bad point is that tags will appears unselected.
            var diff = currentSelectedTags.Where (sTag => workspaceTags.All (wTag => sTag.Name != wTag.Name));

            workspaceTags.AddRange (diff);

            workspaceTags.Sort ((a, b) => {
                var aName = a != null ? (a.Name ?? String.Empty) : String.Empty;
                var bName = b != null ? (b.Name ?? String.Empty) : String.Empty;
                return String.Compare (aName, bName, StringComparison.Ordinal);
            });

            TagCollection.AddRange (workspaceTags);
        }
    }
}
