using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Joey.UI.Fragments
{
    public class ChooseTimeEntryTagsDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntriesIdsArgument = "com.toggl.timer.time_entries_ids";
        private static readonly string WorkspaceArgument = "com.toggl.timer.workspace_id";
        private ListView listView;
        private TagListView viewModel;

        public ChooseTimeEntryTagsDialogFragment ()
        {
        }

        public ChooseTimeEntryTagsDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public ChooseTimeEntryTagsDialogFragment (Guid workspaceId, IList<string> timeEntryIds)
        {
            var args = new Bundle ();
            args.PutString (WorkspaceArgument, workspaceId.ToString ());
            args.PutStringArrayList (TimeEntriesIdsArgument, timeEntryIds);

            Arguments = args;
        }

        private IList<string> TimeEntryIds
        {
            get {
                return Arguments != null ? Arguments.GetStringArrayList (TimeEntriesIdsArgument) : new List<string>();
            }
        }

        private string WorkspaceId
        {
            get {
                return Arguments != null ? Arguments.GetString (WorkspaceArgument) : string.Empty;
            }
        }

        public async override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            if (viewModel == null) {
                viewModel = new TagListView (WorkspaceId, TimeEntryIds);
                viewModel.OnIsLoadingChanged += OnModelLoaded;
                viewModel.Init ();
            }

            if (viewModel.Model.Workspace == null || viewModel.Model.Workspace.Id == Guid.Empty) {
                Dismiss ();
            }
        }

        private void OnModelLoaded (object sender, EventArgs e)
        {
            if (!viewModel.IsLoading) {
                if (viewModel != null) {
                    viewModel.TagList.Updated += OnWorkspaceTagsUpdated;
                    SelectInitialTags ();
                } else {
                    Activity.Finish ();
                }
            }
        }

        private void OnWorkspaceTagsUpdated (object sender, EventArgs args)
        {
            if (!viewModel.TagList.IsLoading) {
                SelectInitialTags ();
            }
        }

        public override void OnDestroy ()
        {
            if (viewModel != null) {
                viewModel.TagList.Updated -= OnWorkspaceTagsUpdated;
                viewModel.Dispose ();
                viewModel = null;
            }

            base.OnDestroy ();
        }

        public override void OnStart ()
        {
            base.OnStart ();
            SelectInitialTags ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            var dia = new AlertDialog.Builder (Activity)
            .SetTitle (Resource.String.ChooseTimeEntryTagsDialogTitle)
            .SetAdapter (new TagsAdapter (viewModel.TagList), (IDialogInterfaceOnClickListener)null)
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
                viewModel.SaveChanges (SelectedTags);

                new CreateTagDialogFragment (WorkspaceId, viewModel.Model).Show (FragmentManager, "new_tag_dialog");
                Dismiss ();
            }
        }

        private void SelectInitialTags ()
        {
            // Select tags
            listView.ClearChoices ();
            foreach (var index in viewModel.SelectedTagsIndex) {
                listView.SetItemChecked (index, true);
            }
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
        }

        private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
        {
            viewModel.SaveChanges (SelectedTags);
        }

        private List<TagData> SelectedTags
        {
            get {
                var selected = listView.CheckedItemPositions;
                return viewModel.TagList.Data
                       .Where ((tag, idx) => selected.Get (idx, false))
                       .ToList ();
            }
        }

    }
}
