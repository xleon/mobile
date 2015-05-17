using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class CreateTagViewModel : IViewModel<ITimeEntryModel>
    {
        private ITimeEntryModel model;
        private bool isLoading;
        private Guid workspaceId;
        private WorkspaceModel workspace;
        private IList<TimeEntryData> timeEntryList;

        public CreateTagViewModel (Guid workspaceId, IList<TimeEntryData> timeEntryList)
        {
            this.workspaceId = workspaceId;
            this.timeEntryList = timeEntryList;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Tag Screen";
        }

        public async void Init ()
        {
            IsLoading = true;

            // Create workspace.
            workspace = new WorkspaceModel (workspaceId);

            // Create model.
            if (timeEntryList.Count > 1) {
                Model = new TimeEntryGroup (timeEntryList);
            } else if (timeEntryList.Count == 1) {
                Model = new TimeEntryModel (timeEntryList [0]);
            }

            // Load models.
            if (timeEntryList.Count > 1) {
                await Task.WhenAll (workspace.LoadAsync (), Model.LoadAsync ());
            } else {
                await workspace.LoadAsync ();
            }

            IsLoading = false;
        }

        public async Task AssignTag (string tagName)
        {
            if (model.Workspace == null) {
                return;
            }

            var store = ServiceContainer.Resolve<IDataStore>();
            var existing = await store.Table<TagData>()
                           .QueryAsync (r => r.WorkspaceId == workspace.Id && r.Name == tagName)
                           .ConfigureAwait (false);

            var checkRelation = true;
            TagModel tag;
            if (existing.Count > 0) {
                tag = new TagModel (existing [0]);
            } else {
                tag = new TagModel {
                    Name = tagName,
                    Workspace = workspace,
                };
                await tag.SaveAsync ().ConfigureAwait (false);

                checkRelation = false;
            }

            if (model != null) {
                var assignTag = true;

                if (checkRelation) {
                    // Check if the relation already exists before adding it
                    var relations = await store.Table<TimeEntryTagData> ()
                                    .CountAsync (r => r.TimeEntryId == model.Id && r.TagId == tag.Id && r.DeletedAt == null)
                                    .ConfigureAwait (false);
                    if (relations < 1) {
                        assignTag = false;
                    }
                }

                if (assignTag) {
                    var relationModel = new TimeEntryTagModel () {
                        TimeEntry = new TimeEntryModel (model.Data),
                        Tag = tag,
                    };
                    await relationModel.SaveAsync ().ConfigureAwait (false);

                    model.Touch ();
                    await model.SaveAsync ().ConfigureAwait (false);
                }
            }
        }

        public void Dispose ()
        {
            model = null;
            workspace = null;
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
    }
}

