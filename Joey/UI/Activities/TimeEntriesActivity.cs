using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Fragments;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true)]
    public class TimeEntriesActivity : BaseActivity, ActionBar.IOnNavigationListener
    {
        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            ActionBar.SetDisplayShowTitleEnabled (false);
            ActionBar.NavigationMode = ActionBarNavigationMode.List;
            var navAdapter = ArrayAdapter.CreateFromResource (this,
                                 Resource.Array.TimeEntriesNavigationList,
                                 Android.Resource.Layout.SimpleSpinnerDropDownItem);
            ActionBar.SetListNavigationCallbacks (navAdapter, this);

            SetContentView (Resource.Layout.TimeEntriesActivity);

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Full);
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
