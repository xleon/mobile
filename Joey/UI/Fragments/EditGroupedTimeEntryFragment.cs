using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;

namespace Toggl.Joey.UI.Fragments
{
    public class EditGroupedTimeEntryFragment : Fragment
    {
        private static readonly string TimeEntriesIdsArgument = "com.toggl.timer.time_entries_ids";

        private EditTimeEntryViewModel viewModel;
        private RecyclerView recyclerView;
        private SimpleEditTimeEntryFragment editTimeEntryFragment;
        private RecyclerView.Adapter adapter;

        public EditGroupedTimeEntryFragment ()
        {
        }

        public EditGroupedTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public EditGroupedTimeEntryFragment (IList<TimeEntryData> timeEntryList)
        {
            var ids = timeEntryList.Select ( t => t.Id.ToString ()).ToList ();

            var args = new Bundle ();
            args.PutStringArrayList (TimeEntriesIdsArgument, ids);
            Arguments = args;

            viewModel = new EditTimeEntryViewModel (timeEntryList);
        }

        private IList<string> TimeEntryIds
        {
            get {
                return Arguments != null ? Arguments.GetStringArrayList (TimeEntriesIdsArgument) : new List<string>();
            }
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.EditGroupedTimeEntryFragment, container, false);
            editTimeEntryFragment = (SimpleEditTimeEntryFragment) ChildFragmentManager.FindFragmentById (Resource.Id.TimeEntryEditChildFragment);
            HasOptionsMenu = true;

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.recyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            recyclerView.AddItemDecoration (new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList));

            return view;
        }

        public override void OnStart ()
        {
            base.OnStart ();

            var extras = Activity.Intent.Extras;
            if (extras == null) {
                Activity.Finish ();
            }

            var extraGuids = extras.GetStringArray (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids);
            viewModel = new EditTimeEntryViewModel (extraGuids);
            viewModel.OnIsLoadingChanged += OnModelLoaded;
            viewModel.Init ();
        }

        public override void OnStop ()
        {
            base.OnStop ();

            if (viewModel != null) {
                viewModel.OnIsLoadingChanged -= OnModelLoaded;
                viewModel.Dispose ();
                viewModel = null;
            }
        }

        private void OnModelLoaded (object sender, EventArgs e)
        {
            if (!viewModel.IsLoading) {
                if (viewModel != null) {
                    editTimeEntryFragment.TimeEntry = viewModel.Model;

                    // Set adapter
                    adapter = new GroupedEditAdapter (viewModel.Model);
                    (adapter as GroupedEditAdapter).HandleTapTimeEntry = HandleTimeEntryClick;
                    recyclerView.SetAdapter (adapter);
                } else {
                    Activity.Finish ();
                }
            }
        }

        protected override void OnProjectEditTextClick (object sender, EventArgs e)
        {
            if (viewModel.Model == null) {
                return;
            }

            var intent = new Intent (Activity, typeof (ProjectListActivity));
            intent.PutStringArrayListExtra (ProjectListActivity.ExtraTimeEntriesIds, TimeEntryIds);
            StartActivity (intent);
        }

        private void HandleTimeEntryClick (TimeEntryData timeEntry)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));
            intent.PutExtra (EditTimeEntryActivity.ExtraTimeEntryId, timeEntry.Id.ToString());
            StartActivity (intent);
        }

    }
}

