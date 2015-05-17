using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class TagListViewModel : IViewModel<ITimeEntryModel>
    {
        private ITimeEntryModel model;
        private WorkspaceTagsView tagList;
        private bool isLoading;
        private Guid workspaceId;
        private IList<TimeEntryData> timeEntryList;
        private List<TimeEntryTagData> modelTags;

        public TagListViewModel (Guid workspaceId, IList<TimeEntryData> timeEntryList)
        {
            this.timeEntryList = timeEntryList;
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Tags";
        }

        public async void Init ()
        {
            IsLoading = true;

            // Create list manager.
            tagList = new WorkspaceTagsView (workspaceId);

            // Create model.
            if (timeEntryList.Count > 1) {
                Model = new TimeEntryGroup (timeEntryList);
            } else if (timeEntryList.Count == 1) {
                Model = new TimeEntryModel (timeEntryList [0]);
            }

            // Create tag list.
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            modelTags = await dataStore.Table<TimeEntryTagData> ()
                        .QueryAsync (r => r.TimeEntryId == model.Id && r.DeletedAt == null);


            Model.PropertyChanged += OnModelPropertyChanged;
            IsLoading = false;
        }

        public async void SaveChanges (List<TagData> selectedTags)
        {
            // Delete unused tag relations:
            var deleteTasks = modelTags
                              .Where (oldTag => !selectedTags.Any (newTag => newTag.Id == oldTag.TagId))
                              .Select (data => new TimeEntryTagModel (data).DeleteAsync ())
                              .ToList();

            // Create new tag relations:
            var createTasks = selectedTags
                              .Where (newTag => !modelTags.Any (oldTag => oldTag.TagId == newTag.Id))
            .Select (data => new TimeEntryTagModel () { TimeEntry = new TimeEntryModel (model.Data), Tag = new TagModel (data) } .SaveAsync ())
            .ToList();

            await Task.WhenAll (deleteTasks.Concat (createTasks));

            if (deleteTasks.Any<Task> () || createTasks.Any<Task> ()) {
                model.Touch ();
                await model.SaveAsync ();
            }
        }

        public void Dispose ()
        {
            Model.PropertyChanged -= OnModelPropertyChanged;
            tagList.Dispose ();
            tagList = null;
            model = null;
        }

        public IList<TimeEntryData> TimeEntryList
        {
            get {
                return timeEntryList;
            }
        }

        public Guid WorkspaceId
        {
            get {
                return workspaceId;
            }
        }

        public IDataView<TagData> TagListDataView
        {
            get {
                return tagList;
            }
        }

        public IList<int> SelectedTagsIndex
        {
            get {
                int count = 0;
                var indexes = new List<int>();

                var modelTagsReady = modelTags != null;
                var workspaceTagsReady = tagList != null && !tagList.IsLoading;

                if (modelTagsReady && workspaceTagsReady) {
                    foreach (var tag in tagList.Data) {
                        if (modelTags.Any (t => t.TagId == tag.Id)) {
                            indexes.Add (count);
                        }
                        count++;
                    }
                }
                return indexes;
            }
        }

        public event EventHandler OnModelChanged;

        public ITimeEntryModel Model
        {
            get {
                return model;
            }

            private set {

                model = value;

                if (OnModelChanged != null) {
                    OnModelChanged (this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler OnIsLoadingChanged;

        public bool IsLoading
        {
            get {
                return isLoading;
            }
            private set {

                if (isLoading  == value) {
                    return;
                }

                isLoading = value;

                if (OnIsLoadingChanged != null) {
                    OnIsLoadingChanged (this, EventArgs.Empty);
                }
            }
        }

        private void OnModelPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == TimeEntryModel.PropertyWorkspace) {
                if (tagList != null) {
                    tagList.WorkspaceId = model.Workspace.Id;
                    workspaceId = model.Workspace.Id;
                }
            }
        }
    }
}

