using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true)]
    public class RecentTimeEntriesActivity : BaseActivity
    {
        protected ListView RecentListView { get; private set; }

        private void FindViews ()
        {
            RecentListView = FindViewById<ListView> (Resource.Id.RecentListView);
        }

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.RecentTimeEntriesActivity);
            FindViews ();

            RecentListView.Adapter = new RecentTimeEntriesAdapter ();

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Full);
        }

        protected override void OnStart ()
        {
            base.OnStart ();

            // Trigger a partial sync, if the sync from OnCreate is still running, it does nothing
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Auto);
        }
    }
}
