using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;

namespace Toggl.Joey.UI.Fragments
{
    public class ChooseTimeEntryTagsDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        private WorkspaceTagsView workspaceTagsView;
        private ListView listView;
        private List<TimeEntryTagData> modelTags;
        private TimeEntryModel model;
        private bool tagsSelected;
        private bool hasStarted;

        public ChooseTimeEntryTagsDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChooseTimeEntryTagsDialogFragment ()
        {
        }

        public ChooseTimeEntryTagsDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        private Guid TimeEntryId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            model = new TimeEntryModel (TimeEntryId);
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
            await model.LoadAsync ();
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

        private async static void ReplaceTags (TimeEntryModel model, List<TimeEntryTagData> modelTags, List<TagData> selectedTags)
        {
            // Delete unused tag relations:
            var deleteTasks = modelTags
                              .Where (oldTag => !selectedTags.Any (newTag => newTag.Id == oldTag.TagId))
                              .Select (data => new TimeEntryTagModel (data).DeleteAsync ())
                              .ToList();

            // Create new tag relations:
            var createTasks = selectedTags
                              .Where (newTag => !modelTags.Any (oldTag => oldTag.TagId == newTag.Id))
            .Select (data => new TimeEntryTagModel () { TimeEntry = model, Tag = new TagModel (data) } .SaveAsync ())
            .ToList();

            await Task.WhenAll (deleteTasks.Concat (createTasks));

            if (deleteTasks.Any<Task> () || createTasks.Any<Task> ()) {
                model.Touch ();
                await model.SaveAsync ();
            }
        }
    }
}
