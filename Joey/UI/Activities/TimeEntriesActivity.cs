using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Fragments;
using ActionBar = Android.Support.V7.App.ActionBar;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true)]
    public class TimeEntriesActivity : BaseActivity, ActionBar.IOnNavigationListener
    {
        private static readonly string SelectedNavIndexExtra = "com.toggl.android.navigation_index";

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            ActionBar.SetDisplayShowTitleEnabled (false);
            ActionBar.NavigationMode = ActionBar.NavigationModeList;
            var navAdapter = ArrayAdapter.CreateFromResource (this,
                                 Resource.Array.TimeEntriesNavigationList,
                                 Android.Resource.Layout.SimpleSpinnerDropDownItem);
            ActionBar.SetListNavigationCallbacks (navAdapter, this);

            SetContentView (Resource.Layout.TimeEntriesActivity);

            if (bundle != null) {
                ActionBar.SetSelectedNavigationItem (bundle.GetInt (SelectedNavIndexExtra));
            }

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Full);
        }

        protected override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);

            outState.PutInt (SelectedNavIndexExtra, ActionBar.SelectedNavigationIndex);
        }

        protected override void OnStart ()
        {
            base.OnStart ();

            // Trigger a partial sync, if the sync from OnCreate is still running, it does nothing
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Auto);
        }

        public bool OnNavigationItemSelected (int position, long id)
        {
            Fragment fragment;
            switch (position) {
            case 0:
                fragment = new RecentTimeEntriesListFragment ();
                break;
            case 1:
                fragment = new LogTimeEntriesListFragment ();
                break;
            default:
                return false;
            }

            var ftx = FragmentManager.BeginTransaction ();
            ftx.Replace (Resource.Id.TimeEntriesListFragmentContainer, fragment);
            ftx.Commit ();

            return true;
        }
    }
}
