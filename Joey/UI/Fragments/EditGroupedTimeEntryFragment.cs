using System;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Views;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;

namespace Toggl.Joey.UI.Fragments
{
    public class EditGroupedTimeEntryFragment : Fragment
    {
        // logica objects
        private EditTimeEntryGroupView viewModel;

        // visual objects
        private RecyclerView recyclerView;
        private SimpleEditTimeEntryFragment editTimeEntryFragment;
        private RecyclerView.Adapter adapter;

        public EditGroupedTimeEntryFragment ()
        {
        }

        public EditGroupedTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.EditGroupedTimeEntryFragment, container, false);
            editTimeEntryFragment = (SimpleEditTimeEntryFragment) FragmentManager.FindFragmentById (Resource.Id.EditTimeEntryModelFragment);
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
            viewModel = new EditTimeEntryGroupView (extraGuids);
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

        private void HandleTimeEntryClick (TimeEntryData timeEntry)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));
            intent.PutExtra (EditTimeEntryActivity.ExtraTimeEntryId, timeEntry.Id.ToString());
            StartActivity (intent);
        }


        private void OnProjectEditTextClick (object sender, EventArgs e)
        {
            if (viewModel.Model == null) {
                return;
            }

            var intent = new Intent (Activity, typeof (ProjectListActivity));
            intent.PutExtra (ProjectListActivity.ExtraTimeEntriesIds, viewModel.Model.TimeEntryGuids);
            StartActivity (intent);
        }

    }
}

