using System;
using Android.OS;
using Toggl.Joey.UI.Activities;
using Toggl.Phoebe.Data.Views;
using Fragment = Android.Support.V4.App.Fragment;
using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;
using Android.Content;

namespace Toggl.Joey.UI.Fragments
{
    public class EditTimeEntryFragment : BaseEditTimeEntryFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        private static readonly string UseDraftKey = "com.toggl.timer.draft_used";
        private EditTimeEntryView viewModel;

        public EditTimeEntryFragment ()
        {
        }

        public EditTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public EditTimeEntryFragment (TimeEntryData timeEntry)
        {
            Arguments = new Bundle ();
            Arguments.PutString (TimeEntryIdArgument, timeEntry.Id.ToString ());
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

        public override void OnViewCreated (Android.Views.View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            bool useDraft = false;
            if (savedInstanceState != null) {
                useDraft = savedInstanceState.GetBoolean (UseDraftKey, useDraft);
            }

            var extras = Activity.Intent.Extras;
            if (extras != null) {
                var extraGuid = extras.GetString (EditTimeEntryActivity.ExtraTimeEntryId);
                Arguments.PutString (TimeEntryIdArgument, extraGuid);
            }

            viewModel = new EditTimeEntryView (TimeEntryId, useDraft);
            viewModel.OnIsLoadingChanged += OnModelLoaded;
            viewModel.Init ();
        }

        private void OnModelLoaded (object sender, EventArgs e)
        {
            if (!viewModel.IsLoading) {
                if (viewModel != null) {
                    TimeEntry = viewModel.Model;
                    viewModel.OnModelChanged += OnModelChanged;
                    OnPressedProjectSelector += OnProjectSelected;
                    OnPressedTagSelector += OnTagSelected;
                } else {
                    Activity.Finish ();
                }
            }
        }

        private void OnModelChanged (object sender, EventArgs e)
        {
            TimeEntry = viewModel.Model;
        }

        private void OnProjectSelected (object sender, EventArgs e)
        {
            if (TimeEntry == null) {
                return;
            }

            var intent = new Intent (Activity, typeof (ProjectListActivity));
            intent.PutStringArrayListExtra (ProjectListActivity.ExtraTimeEntriesIds, new List<string> {TimeEntry.Id.ToString ()});
            StartActivity (intent);
        }

        private void OnTagSelected (object sender, EventArgs e)
        {
            if (TimeEntry == null) {
                return;
            }
            new ChooseTimeEntryTagsDialogFragment (TimeEntry.Workspace.Id, new List<TimeEntryData> {TimeEntry.Data}).Show (FragmentManager, "tags_dialog");
        }

        public override void OnDestroy ()
        {
            if (viewModel != null) {
                viewModel.OnIsLoadingChanged -= OnModelLoaded;
                viewModel.OnModelChanged -= OnModelChanged;
                viewModel.Dispose ();
            }

            base.OnDestroy ();
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            outState.PutBoolean (UseDraftKey, viewModel.IsDraft);
            base.OnSaveInstanceState (outState);
        }

        protected override void ResetModel ()
        {
            viewModel.ResetModel ();
        }
    }
}
