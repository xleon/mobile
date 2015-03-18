
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Toggl.Joey.UI.Adapters;

using Toolbar = Android.Support.V7.Widget.Toolbar;
using Fragment = Android.Support.V4.App.Fragment;
using Activity = Android.Support.V7.App.ActionBarActivity;
using ActionBar = Android.Support.V7.App.ActionBar;

namespace Toggl.Joey.UI.Fragments
{
    public class ProjectsFragment : Fragment
    {
        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);


            // Create your fragment here
        }

        protected RecyclerView RecyclerView { get; private set; }
        protected RecyclerView.Adapter Adapter { get; private set; }
        protected RecyclerView.LayoutManager LayoutManager { get; private set; }

        protected ActionBar Toolbar { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {

         //   ProjectsFragmentToolbarRecyclerView

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
            //Toolbar.SetHomeButtonEnabled (true);
            Toolbar.SetDisplayHomeAsUpEnabled (true);
            Toolbar.SetTitle (Resource.String.ProjectsTitle);


            HasOptionsMenu = true;

            return view;



        }


        public override void OnOptionsMenuClosed (IMenu menu)
        {
            base.OnOptionsMenuClosed (menu);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                Activity.OnBackPressed ();
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

