using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Joey.UI.Adapters;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.ActionBarActivity;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class ProjectListFragment : Fragment
    {
        protected RecyclerView RecyclerView { get; private set; }
        protected RecyclerView.Adapter Adapter { get; private set; }
        protected RecyclerView.LayoutManager LayoutManager { get; private set; }

        protected ActionBar Toolbar { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ProjectListFragment, container, false);

            RecyclerView = view.FindViewById<RecyclerView> (Resource.Id.ProjectListRecyclerView);
            LayoutManager = new LinearLayoutManager (Activity);
            RecyclerView.SetLayoutManager (LayoutManager);

            Adapter = new ProjectListAdapter ();
            RecyclerView.SetAdapter (Adapter);

            var activity = (ActionBarActivity)Activity;

            var toolbar = view.FindViewById<Toolbar> (Resource.Id.ProjectListToolbar);
            activity.SetSupportActionBar (toolbar);
            Toolbar = activity.SupportActionBar;
            Toolbar.SetDisplayHomeAsUpEnabled (true);
            Toolbar.SetTitle (Resource.String.ProjectsTitle);

            HasOptionsMenu = true;

            return view;
        }

        private void StartNewProjectActivity ()
        {
            /*
            var intent = new Intent (Activity, typeof (NewProjectActivity));
            intent.PutExtra (NewProjectActivity.ExtraWorkspaceId, (Adapter as ProjectListAdapter).Workspace.Id.ToString ());
            StartActivity (intent);
            */
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                Activity.OnBackPressed ();
            } else {
                StartNewProjectActivity ();
            }
            return base.OnOptionsItemSelected (item);
        }

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate (Resource.Menu.ProjectToolbarHome, menu);
            base.OnCreateOptionsMenu (menu, inflater);
        }

    }
}

