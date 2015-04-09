using Android.Content;
using Android.OS;
using Android.Views;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Activities;

using Toolbar = Android.Support.V7.Widget.Toolbar;
using Fragment = Android.Support.V4.App.Fragment;
using Activity = Android.Support.V7.App.ActionBarActivity;
using ActionBar = Android.Support.V7.App.ActionBar;

namespace Toggl.Joey.UI.Fragments
{
    public class ProjectsFragment : Fragment
    {
        protected RecyclerView RecyclerView { get; private set; }
        protected RecyclerView.Adapter Adapter { get; private set; }
        protected RecyclerView.LayoutManager LayoutManager { get; private set; }

        protected ActionBar Toolbar { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ProjectsFragment, container, false);

            RecyclerView = view.FindViewById<RecyclerView> (Resource.Id.ProjectsFragmentToolbarRecyclerView);
            LayoutManager = new LinearLayoutManager (Activity);
            RecyclerView.SetLayoutManager (LayoutManager);

            Adapter = new ProjectsRecyclerAdapter ();
            RecyclerView.SetAdapter (Adapter);

            var activity = (Android.Support.V7.App.ActionBarActivity)this.Activity;

            var toolbar = view.FindViewById<Toolbar> (Resource.Id.ProjectsFragmentToolbar);
            activity.SetSupportActionBar (toolbar);
            Toolbar = activity.SupportActionBar;
            Toolbar.SetDisplayHomeAsUpEnabled (true);
            Toolbar.SetTitle (Resource.String.ProjectsTitle);

            HasOptionsMenu = true;

            return view;
        }

        private void StartNewProjectActivity ()
        {
            var intent = new Intent (Activity, typeof (NewProjectActivity));
            intent.PutExtra (NewProjectActivity.ExtraWorkspaceId, (Adapter as ProjectsRecyclerAdapter).Workspace.Id.ToString ());
            StartActivity (intent);
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

