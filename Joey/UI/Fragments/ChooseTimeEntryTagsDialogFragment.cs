using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class ChooseTimeEntryTagsDialogFragment : BaseDialogFragment
    {
        private static readonly string ModelIds = "com.toggl.timer.model_ids";
        private WorkspaceTagsView workspaceTagsView;
        private ListView listView;
        private List<TimeEntryTagData> modelTags;
        private ITimeEntryModel model;
        private bool tagsSelected;
        private bool hasStarted;

        public ChooseTimeEntryTagsDialogFragment (ITimeEntryModel model)
        {
            var args = new Bundle ();

            args.PutStringArrayList (ModelIds, model.Ids);
            Arguments = args;

            this.model = model;
        }

        public ChooseTimeEntryTagsDialogFragment ()
        {
        }

        public ChooseTimeEntryTagsDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        private List<Guid> Guids
        {
            get {
                var ids = new List<Guid> ();
                if (Arguments != null) {
                    var list = Arguments.GetStringArrayList (ModelIds);
                    if (list != null && list.Count > 0) {
                        foreach (var guidEntity in list) {
                            var guidParsing = Guid.Empty;
                            Guid.TryParse (guidEntity, out guidParsing);
                            if (guidParsing != Guid.Empty) {
                                ids.Add (guidParsing);
                            }
                        }
                    }
                }
                return ids;
            }
        }

        public async override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            if (model == null) {
                var guids = Guids;
                if (guids.Count <= 1) {
                    model = new TimeEntryModel (guids.First ());
                } else {
                    var grp = new TimeEntryGroup ();
                    await grp.BuildFromGuids (guids);
                    model = grp;
                }
            }

            model.PropertyChanged += OnModelPropertyChanged;

            workspaceTagsView = new WorkspaceTagsView (WorkspaceId);
            workspaceTagsView.Updated += OnWorkspaceTagsUpdated;

            LoadModel ();
            LoadTags ();
        }

        public override void OnDestroy ()
        {
            if (model != null) {
                model.PropertyChanged -= OnModelPropertyChanged;
                model = null;
            }
            if (workspaceTagsView != null) {
                workspaceTagsView.Updated -= OnWorkspaceTagsUpdated;
                workspaceTagsView.Dispose ();
                workspaceTagsView = null;
            }

            base.OnDestroy ();
        }

        private async void LoadModel ()
        {
            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                Dismiss ();
            }
        }

        private async void LoadTags ()
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            modelTags = await dataStore.Table<TimeEntryTagData> ()
                        .QueryAsync (r => r.TimeEntryId == model.Id && r.DeletedAt == null);
            SelectInitialTags ();
        }

        private void OnModelPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == TimeEntryModel.PropertyWorkspace) {
                if (workspaceTagsView != null) {
                    workspaceTagsView.WorkspaceId = model.Workspace.Id;
                }
            }
        }

        private void OnWorkspaceTagsUpdated (object sender, EventArgs args)
        {
            if (!workspaceTagsView.IsLoading) {
                SelectInitialTags ();
            }
        }

        private Guid WorkspaceId
        {
            get {
                if (model != null && model.Workspace != null) {
                    return model.Workspace.Id;
                }
                return Guid.Empty;
            }
        }

        public override Dialog OnCreateDialog (Bundle state)
        {
            var dia = new AlertDialog.Builder (Activity)
            .SetTitle (Resource.String.ChooseTimeEntryTagsDialogTitle)
            .SetAdapter (new TagsAdapter (workspaceTagsView), (IDialogInterfaceOnClickListener)null)
            .SetNegativeButton (Resource.String.ChooseTimeEntryTagsDialogCancel, OnCancelButtonClicked)
            .SetPositiveButton (Resource.String.ChooseTimeEntryTagsDialogOk, OnOkButtonClicked)
            .Create ();

            listView = dia.ListView;
            listView.ItemsCanFocus = false;
            listView.ChoiceMode = ChoiceMode.Multiple;
            listView.ItemClick += OnItemClick;

            return dia;
        }

        private void OnItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            if (e.Id == TagsAdapter.CreateTagId) {
                // Commit changes the user has made thusfar
                ReplaceTags (model, modelTags, SelectedTags);

                new CreateTagDialogFragment (WorkspaceId, model).Show (FragmentManager, "new_tag_dialog");
                Dismiss ();
            }
        }

        private void SelectInitialTags ()
        {
            var modelTagsReady = modelTags != null;
            var workspaceTagsReady = workspaceTagsView != null && !workspaceTagsView.IsLoading;

            if (tagsSelected || !hasStarted || !modelTagsReady || !workspaceTagsReady) {
                return;
            }

            // Select tags
            var i = 0;
            listView.ClearChoices ();
            foreach (var tag in workspaceTagsView.Data) {
                if (modelTags.Any (t => t.TagId == tag.Id)) {
                    listView.SetItemChecked (i, true);
                }
                i++;
            }

            tagsSelected = true;
        }

        public override void OnStart ()
        {
            base.OnStart ();

            hasStarted = true;
            SelectInitialTags ();

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Tags";
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
        }

        private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
        {
            ReplaceTags (model, modelTags, SelectedTags);
        }

        private List<TagData> SelectedTags
        {
            get {
                var selected = listView.CheckedItemPositions;
                return workspaceTagsView.Data
                       .Where ((tag, idx) => selected.Get (idx, false))
                       .ToList ();
            }
        }

        private async static void ReplaceTags (ITimeEntryModel model, List<TimeEntryTagData> modelTags, List<TagData> selectedTags)
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            await model.Apply (async delegate (TimeEntryModel m) {
                var mTags = await dataStore.Table<TimeEntryTagData> ()
                            .QueryAsync (r => r.TimeEntryId == m.Id && r.DeletedAt == null);
                var deleteTasks =  mTags.Where (oldTag => selectedTags.All (newTag => newTag.Id != oldTag.TagId))
                                   .Select (data => new TimeEntryTagModel (data).DeleteAsync()).ToList();
                var createTasks = selectedTags
                                  .Where (newTag => mTags.All (oldTag => oldTag.TagId != newTag.Id))
                .Select (data => new TimeEntryTagModel () { TimeEntry = m, Tag = new TagModel (data) } .SaveAsync ())
                .ToList();
                await Task.WhenAll (deleteTasks.Concat (createTasks));
            });
        }
    }
}
