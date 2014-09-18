using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
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
        private bool isSaving;

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

        private Guid TimeEntryId {
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

        private Guid WorkspaceId {
            get {
                if (model != null && model.Workspace != null)
                    return model.Workspace.Id;
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
            // Reset the item click listener such that the dialog wouldn't be closed on selecting a tag
            listView.OnItemClickListener = null;

            return dia;
        }

        private void SelectInitialTags ()
        {
            var modelTagsReady = modelTags != null;
            var workspaceTagsReady = workspaceTagsView != null && !workspaceTagsView.IsLoading;

            if (tagsSelected || !hasStarted || !modelTagsReady || !workspaceTagsReady)
                return;

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
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
            Dismiss ();
        }

        private async void OnOkButtonClicked (object sender, DialogClickEventArgs args)
        {
            if (isSaving)
                return;

            isSaving = true;
            try {
                // Store the model reference so it wouldn't get nulled by OnDestroy while doing async things.
                var model = this.model;

                // Resolve selected indexes into TagData:
                var selected = listView.CheckedItemPositions;
                var tags = workspaceTagsView.Data
                    .Where ((tag, idx) => selected.Get (idx, false))
                    .ToList ();

                // Delete unused tag relations:
                var deleteTasks = modelTags
                    .Where (oldTag => !tags.Any (newTag => newTag.Id == oldTag.TagId))
                    .Select (data => new TimeEntryTagModel (data).DeleteAsync ());

                // Create new tag relations:
                var createTasks = tags
                    .Where (newTag => !modelTags.Any (oldTag => oldTag.TagId == newTag.Id))
                    .Select (data => new TimeEntryTagModel () { TimeEntry = model, Tag = new TagModel (data) }.SaveAsync ());

                await Task.WhenAll (deleteTasks.Concat (createTasks));

                if (deleteTasks.Any<Task> () || createTasks.Any<Task> ()) {
                    model.Touch ();
                    await model.SaveAsync ();
                }

                Dismiss ();
            } finally {
                isSaving = false;
            }
        }
    }
}
