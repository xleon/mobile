using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Text.Format;
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
        private const string PageStackExtra = "com.toggl.timer.page_stack";
        private readonly TimerComponent barTimer = new TimerComponent ();
        private readonly Lazy<TimeTrackingFragment> trackingFragment = new Lazy<TimeTrackingFragment> ();
        private readonly Lazy<SettingsListFragment> settingsFragment = new Lazy<SettingsListFragment> ();
        private readonly List<int> pageStack = new List<int> ();
        private readonly Handler handler = new Handler ();
        private DrawerListAdapter drawerAdapter;
        private ImageButton syncRetryButton;
        private TextView syncStatusText;
        private DateTime lastSyncTime;
        private Subscription<SyncStartedMessage> drawerSyncStarted;
        private Subscription<SyncFinishedMessage> drawerSyncFinished;

        private ListView DrawerListView { get; set; }

        private DrawerLayout DrawerLayout { get; set; }

        protected ActionBarDrawerToggle DrawerToggle { get; private set; }

        private FrameLayout DrawerSyncView { get; set; }

        protected override void OnCreateActivity (Bundle bundle)
        {
            base.OnCreateActivity (bundle);

            SetContentView (Resource.Layout.MainDrawerActivity);

            DrawerListView = FindViewById<ListView> (Resource.Id.DrawerListView);
            DrawerListView.Adapter = drawerAdapter = new DrawerListAdapter ();
            DrawerListView.ItemClick += OnDrawerListViewItemClick;

            DrawerLayout = FindViewById<DrawerLayout> (Resource.Id.DrawerLayout);
            DrawerToggle = new ActionBarDrawerToggle (this, DrawerLayout, Resource.Drawable.IcDrawer, Resource.String.EntryName, Resource.String.EntryName);

            DrawerLayout.SetDrawerShadow (Resource.Drawable.drawershadow, (int)GravityFlags.Start);
            DrawerLayout.SetDrawerListener (DrawerToggle);

            Timer.OnCreate (this);
            var lp = new ActionBar.LayoutParams (ActionBar.LayoutParams.WrapContent, ActionBar.LayoutParams.WrapContent);
            lp.Gravity = GravityFlags.Right | GravityFlags.CenterVertical;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            drawerSyncStarted = bus.Subscribe<SyncStartedMessage> (SyncStarted);
            drawerSyncFinished = bus.Subscribe<SyncFinishedMessage> (SyncFinished);

            DrawerSyncView = FindViewById<FrameLayout> (Resource.Id.DrawerSyncStatus);

            syncRetryButton = DrawerSyncView.FindViewById<ImageButton> (Resource.Id.SyncRetryButton);
            syncStatusText = DrawerSyncView.FindViewById<TextView> (Resource.Id.SyncStatusText);
            syncRetryButton.Click += OnSyncRetryClick;
            UpdateSyncStatus ();

            ActionBar.SetCustomView (Timer.Root, lp);
            ActionBar.SetDisplayShowCustomEnabled (true);
            ActionBar.SetDisplayHomeAsUpEnabled (true);
            ActionBar.SetHomeButtonEnabled (true);

            if (bundle == null) {
                OpenPage (DrawerListAdapter.TimerPageId);
            } else {
                // Restore page stack
                pageStack.Clear ();
                var arr = bundle.GetIntArray (PageStackExtra);
                if (arr != null) {
                    pageStack.AddRange (arr);
                }
            }
        }

        protected override void OnSaveInstanceState (Bundle outState)
        {
            outState.PutIntArray (PageStackExtra, pageStack.ToArray ());
            base.OnSaveInstanceState (outState);
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

        protected override void OnStop ()
        {
            base.OnStop ();
            Timer.OnStop ();
        }

        public override void OnBackPressed ()
        {
            if (pageStack.Count > 0) {
                pageStack.RemoveAt (pageStack.Count - 1);
                var pageId = pageStack.Count > 0 ? pageStack [pageStack.Count - 1] : DrawerListAdapter.TimerPageId;
                OpenPage (pageId);
            } else {
                base.OnBackPressed ();
            }
        }

        private void OpenPage (int id)
        {
            // Configure timer component for selected page:
            if (id != DrawerListAdapter.TimerPageId) {
                Timer.HideAction = true;
                Timer.HideDuration = false;
            } else {
                Timer.HideAction = false;
            }

            if (id == DrawerListAdapter.SettingsPageId) {
                DrawerListView.SetItemChecked (drawerAdapter.GetItemPosition (DrawerListAdapter.SettingsPageId), true);
                OpenFragment (settingsFragment.Value);
            } else {
                DrawerListView.SetItemChecked (drawerAdapter.GetItemPosition (DrawerListAdapter.TimerPageId), true);
                OpenFragment (trackingFragment.Value);
            }

            pageStack.Remove (id);
            pageStack.Add (id);
            // Make sure we don't store the timer page as the first page (this is implied)
            if (pageStack.Count == 1 && id == DrawerListAdapter.TimerPageId) {
                pageStack.Clear ();
            }
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

        private void OnDrawerListViewItemClick (object sender, ListView.ItemClickEventArgs e)
        {
            if (e.Id == DrawerListAdapter.TimerPageId) {
                OpenPage (DrawerListAdapter.TimerPageId);

            } else if (e.Id == DrawerListAdapter.LogoutPageId) {
                var authManager = ServiceContainer.Resolve<AuthManager> ();
                authManager.Forget ();
                StartAuthActivity ();

            } else if (e.Id == DrawerListAdapter.SettingsPageId) {
                OpenPage (DrawerListAdapter.SettingsPageId);

            }

            DrawerLayout.CloseDrawers ();
        }

        private bool isSyncing { get; set; }

        protected void SyncStarted (SyncStartedMessage msg)
        {
            syncRetryButton.Enabled = false;
            isSyncing = true;
            syncStatusText.SetText (Resource.String.CurrentlySyncingStatusText);
        }

        private void SyncFinished (SyncFinishedMessage msg)
        {
            syncRetryButton.Enabled = true;
            isSyncing = false;
            lastSyncTime = Toggl.Phoebe.Time.Now;
        }

        private void OnSyncRetryClick (object sender, EventArgs e)
        {
            SyncManager SyncManager = ServiceContainer.Resolve<SyncManager> ();
            SyncManager.Run ();
        }

        private void UpdateSyncStatus ()
        {
            if (isSyncing) {
                syncStatusText.SetText (Resource.String.CurrentlySyncingStatusText);
            } else {
                long NowInMillis = Toggl.Phoebe.Time.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long LastSyncInMillis = lastSyncTime.Ticks / TimeSpan.TicksPerMillisecond;
                syncStatusText.Text = "Last sync " + DateUtils.GetRelativeTimeSpanString (LastSyncInMillis, NowInMillis, 0L);
            }
            handler.RemoveCallbacks (UpdateSyncStatus);
            handler.PostDelayed (UpdateSyncStatus, 1000);
        }

        public TimerComponent Timer {
            get { return barTimer; }
        }
    }
}
