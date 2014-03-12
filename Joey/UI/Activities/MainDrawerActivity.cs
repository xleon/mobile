using System;
using Android.App;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        Exported = true,
        #if DEBUG
        // The actual entry-point is defined in manifest via activity-alias, this here is just to
        // make adb launch the activity automatically when developing.
        MainLauncher = true,
        #endif
        Theme = "@style/Theme.Toggl.App")]
    public class MainDrawerActivity : BaseActivity
    {
        private static readonly string LogTag = "MainDrawerActivity";
        protected Logger log;
        private readonly TimerComponent barTimer = new TimerComponent ();
        private DrawerLayout DrawerLayout;
        private readonly Lazy<TimeTrackingFragment> trackingFragment = new Lazy<TimeTrackingFragment> ();
        private readonly Lazy<SettingsFragment> settingsFragment = new Lazy<SettingsFragment> ();
        protected ActionBarDrawerToggle DrawerToggle;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.MainDrawerActivity);

            var drawerList = FindViewById<ListView> (Resource.Id.DrawerListView);
            drawerList.Adapter = new DrawerListAdapter ();
            drawerList.ItemClick += OnDrawerItemClick;

            DrawerLayout = FindViewById<DrawerLayout> (Resource.Id.DrawerLayout);
            DrawerToggle = new ActionBarDrawerToggle (this, DrawerLayout, Resource.Drawable.IcDrawer, Resource.String.EntryName, Resource.String.EntryName);

            DrawerLayout.SetDrawerShadow (Resource.Drawable.drawershadow, (int)GravityFlags.Start);
            DrawerLayout.SetDrawerListener (DrawerToggle);

            Timer.OnCreate (this);
            var lp = new ActionBar.LayoutParams (ActionBar.LayoutParams.WrapContent, ActionBar.LayoutParams.WrapContent);
            lp.Gravity = GravityFlags.Right | GravityFlags.CenterVertical;

            ActionBar.SetCustomView (Timer.Root, lp);
            ActionBar.SetDisplayShowCustomEnabled (true);
            ActionBar.SetDisplayHomeAsUpEnabled (true);
            ActionBar.SetHomeButtonEnabled (true);

            OpenTimeTracking ();

            log = ServiceContainer.Resolve<Logger> ();
        }

        protected override void OnPostCreate (Bundle savedInstanceState)
        {
            base.OnPostCreate (savedInstanceState);
            DrawerToggle.SyncState ();
        }

        public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged (newConfig);
            DrawerToggle.OnConfigurationChanged (newConfig);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (DrawerToggle.OnOptionsItemSelected (item)) {
                return true;
            }

            return base.OnOptionsItemSelected (item);
        }

        protected override void OnStart ()
        {
            base.OnStart ();
            Timer.OnStart ();
        }

        protected override void OnResume ()
        {
            base.OnResume ();
        }

        protected override void OnStop ()
        {
            base.OnStop ();
            Timer.OnStop ();
        }

        private void OpenTimeTracking ()
        {
            OpenFragment (trackingFragment.Value);
        }

        private void OpenSettings ()
        {
            OpenFragment (settingsFragment.Value);
        }

        private void OpenFragment (Fragment fragment)
        {
            var old = FragmentManager.FindFragmentById (Resource.Id.ContentFrameLayout);
            if (old == null) {
                FragmentManager.BeginTransaction ()
                    .Add (Resource.Id.ContentFrameLayout, fragment)
                    .Commit ();
            } else {
                // The detach/attach is a workaround for https://code.google.com/p/android/issues/detail?id=42601
                FragmentManager.BeginTransaction ()
                    .Detach (old)
                    .Replace (Resource.Id.ContentFrameLayout, fragment)
                    .Attach (fragment)
                    .Commit ();
            }
        }

        private void OnDrawerItemClick (object sender, ListView.ItemClickEventArgs e)
        {
            log.Debug (LogTag, "Drawer item clicked " + e.Id);

            // Configure timer component for selected page:
            if (e.Id != DrawerListAdapter.TimerPageId) {
                Timer.HideAction = true;
                Timer.HideDuration = false;
            } else {
                Timer.HideAction = false;
            }

            if (e.Id == DrawerListAdapter.TimerPageId) {
                OpenTimeTracking ();

            } else if (e.Id == DrawerListAdapter.LogoutPageId) {
                var authManager = ServiceContainer.Resolve<AuthManager> ();
                authManager.Forget ();
                CheckAuth ();

            } else if (e.Id == DrawerListAdapter.SettingsPageId) {
                OpenSettings ();

            }

            DrawerLayout.CloseDrawers ();
        }

        public TimerComponent Timer {
            get { return barTimer; }
        }
    }
}

