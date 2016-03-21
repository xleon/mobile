using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using XPlatUtils;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Phoebe._ViewModels
{
    public interface IOnTagSelectedHandler
    {
        void OnCreateNewTag (TagData newTagData);

        void OnModifyTagList (List<TagData> newTagList);
    }

    public class TagListVM : IDisposable
    {
        // This viewMode is apparently simple but
        // it needs the code related with the update of
        // the list. (subscription and reload of data
        // everytime a tag is changed/created/deleted

        public TagListVM (AppState appState, Guid workspaceId, List<Guid> previousSelectedIds)
        {
            TagCollection = LoadTags (appState, workspaceId, previousSelectedIds);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Tags";
        }

        public void Dispose ()
        {
            TagCollection = null;
        }

        public ObservableRangeCollection<TagData> TagCollection { get; set; }

        private ObservableRangeCollection<TagData> LoadTags (
            AppState appState, Guid workspaceId, List<Guid> previousSelectedIds)
        {
            var tagCollection = new ObservableRangeCollection<TagData> ();

            var selectedTags =
                appState.Tags.Values.Where (
                    r => r.WorkspaceId == workspaceId &&
                    previousSelectedIds.Contains (r.Id)).ToList ();

            selectedTags.Sort (
                (a, b) => string.Compare (a?.Name ?? "", b?.Name ?? "", StringComparison.Ordinal));

            tagCollection.AddRange (selectedTags);
            return tagCollection;
        }
    }
}
