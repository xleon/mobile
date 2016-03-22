using System;
using System.Collections.Generic;
using System.ComponentModel;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Text.Format;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Fragments;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
         ScreenOrientation = ScreenOrientation.Portrait,
         Name = "toggl.joey.ui.activities.MainDrawerActivity",
         Label = "@string/EntryName",
         Exported = true,
         #if DEBUG
         // The actual entry-point is defined in manifest via activity-alias, this here is just to
         // make adb launch the activity automatically when developing.
         #endif
         Theme = "@style/Theme.Toggl.Main")]
    public class MainDrawerActivity : BaseActivity
    {
        private const string PageStackExtra = "com.toggl.timer.page_stack";
        private const string LastSyncArgument = "com.toggl.timer.last_sync";
        private const string LastSyncResultArgument = "com.toggl.timer.last_sync_result";
        private readonly TimerComponent barTimer = new TimerComponent ();
        private readonly Lazy<LogTimeEntriesListFragment> trackingFragment = new Lazy<LogTimeEntriesListFragment> ();
        private readonly Lazy<SettingsListFragment> settingsFragment = new Lazy<SettingsListFragment> ();
        private readonly Lazy<ReportsPagerFragment> reportFragment = new Lazy<ReportsPagerFragment> ();
        private readonly Lazy<FeedbackFragment> feedbackFragment = new Lazy<FeedbackFragment> ();
        private readonly Lazy<RegisterUserFragment> registerUserFragment = new Lazy<RegisterUserFragment> ();
        private readonly List<int> pageStack = new List<int> ();
        private readonly Handler handler = new Handler ();
        private AuthManager authManager;
        private DrawerListAdapter drawerAdapter;
        private ToolbarModes toolbarMode;

        private ListView DrawerListView { get; set; }

        private TextView DrawerUserName { get; set; }

        private TextView DrawerEmail { get; set; }

        private ProfileImageView DrawerImage { get; set; }

        private View DrawerUserView { get; set; }

        private DrawerLayout DrawerLayout { get; set; }

        protected ActionBarDrawerToggle DrawerToggle { get; private set; }

        private FrameLayout DrawerSyncView { get; set; }

        public Toolbar MainToolbar { get; set; }


        // Explanation of native constructor
        // http://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview/10603714#10603714
        public MainDrawerActivity (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        public MainDrawerActivity ()
        {
        }

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.MainDrawerActivity);

            MainToolbar = FindViewById<Toolbar> (Resource.Id.MainToolbar);
            DrawerListView = FindViewById<ListView> (Resource.Id.DrawerListView);
            DrawerUserView = LayoutInflater.Inflate (Resource.Layout.MainDrawerListHeader, null);
            DrawerUserName = DrawerUserView.FindViewById<TextView> (Resource.Id.TitleTextView);
            DrawerEmail = DrawerUserView.FindViewById<TextView> (Resource.Id.EmailTextView);
            DrawerImage = DrawerUserView.FindViewById<ProfileImageView> (Resource.Id.IconProfileImageView);
            DrawerListView.Adapter = drawerAdapter = new DrawerListAdapter ();
            DrawerListView.ItemClick += OnDrawerListViewItemClick;

            authManager = ServiceContainer.Resolve<AuthManager> ();
            authManager.PropertyChanged += OnUserChangedEvent;

            DrawerLayout = FindViewById<DrawerLayout> (Resource.Id.DrawerLayout);
            DrawerToggle = new ActionBarDrawerToggle (this, DrawerLayout, MainToolbar, Resource.String.EntryName, Resource.String.EntryName);

            DrawerLayout.SetDrawerShadow (Resource.Drawable.drawershadow, (int)GravityFlags.Start);
            DrawerLayout.SetDrawerListener (DrawerToggle);

            var drawerFrameLayout = FindViewById<FrameLayout> (Resource.Id.DrawerFrameLayout);
            drawerFrameLayout.Touch += (sender, e) => {
                // Do nothing, just absorb the event
                // TODO: Improve this dirty solution?
            };

            if (!ServiceContainer.Resolve<AuthManager> ().OfflineMode) {
                DrawerListView.AddHeaderView (DrawerUserView);
            }
            Timer.OnCreate (this);

            var lp = new Android.Support.V7.App.ActionBar.LayoutParams (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent, (int)GravityFlags.Right);

            MainToolbar.SetNavigationIcon (Resource.Drawable.ic_menu_black_24dp);
            SetSupportActionBar (MainToolbar);
            SupportActionBar.SetTitle (Resource.String.MainDrawerTimer);
            SupportActionBar.SetCustomView (Timer.Root, lp);
            SupportActionBar.SetDisplayShowCustomEnabled (true);

            if (state == null) {
                OpenPage (DrawerListAdapter.TimerPageId);
            } else {
                // Restore page stack
                pageStack.Clear ();
                var arr = state.GetIntArray (PageStackExtra);
                if (arr != null) {
                    pageStack.AddRange (arr);
                }
            }

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<ISyncManager> ().Run ();
        }

        public ToolbarModes ToolbarMode
        {
            get {
                return toolbarMode;
            } set {
                toolbarMode = value;
                AdjustToolbar();
            }
        }

        public void OpenSubView (Fragment frag, string tag)
        {
            FragmentManager.BeginTransaction ()
            .Add (frag, tag)
            .AddToBackStack (tag)
            .Attach (frag)
            .Commit ();
            OpenFragment (frag);
        }

        private void AdjustToolbar()
        {
            switch (toolbarMode) {
            case MainDrawerActivity.ToolbarModes.Timer:
                SupportActionBar.SetDisplayShowTitleEnabled (false);
                MainToolbar.Visibility = ViewStates.Visible;
                Timer.Hide = false;
                break;
            case MainDrawerActivity.ToolbarModes.Normal:
                MainToolbar.Visibility = ViewStates.Visible;
                Timer.Hide = true;
                SupportActionBar.SetDisplayShowTitleEnabled (true);
                break;
            case MainDrawerActivity.ToolbarModes.SubView:
                MainToolbar.Visibility = ViewStates.Gone;
                break;
            }
        }

        private void OnUserChangedEvent (object sender, PropertyChangedEventArgs args)
        {
            var userData = ServiceContainer.Resolve<AuthManager> ().User;
            if (userData == null) {
                return;
            }
            DrawerUserName.Text = userData.Name;
            DrawerEmail.Text = userData.Email;
            DrawerImage.ImageUrl = userData.ImageUrl;
        }

        protected override void OnSaveInstanceState (Bundle outState)
        {
            outState.PutIntArray (PageStackExtra, pageStack.ToArray ());
            base.OnSaveInstanceState (outState);
        }

        public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged (newConfig);
            DrawerToggle.OnConfigurationChanged (newConfig);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (ToolbarMode == ToolbarModes.SubView && item.ItemId == Android.Resource.Id.Home) {
                OnBackPressed ();
                return true;
            }
            return DrawerToggle.OnOptionsItemSelected (item) || base.OnOptionsItemSelected (item);
        }

        private void SetMenuSelection (int pos)
        {
            DrawerListView.ClearChoices ();
            DrawerListView.ChoiceMode = (ChoiceMode)ListView.ChoiceModeSingle;
            DrawerListView.SetItemChecked (pos, true);
        }

        public void OpenPage (int id)
        {
            if (id == DrawerListAdapter.SettingsPageId) {
                OpenFragment (settingsFragment.Value);
                SupportActionBar.SetTitle (Resource.String.MainDrawerSettings);
            } else if (id == DrawerListAdapter.ReportsPageId) {
                if (reportFragment.Value.ZoomLevel == ZoomLevel.Week) {
                    SupportActionBar.SetTitle (Resource.String.MainDrawerReportsWeek);
                } else if (reportFragment.Value.ZoomLevel == ZoomLevel.Month) {
                    SupportActionBar.SetTitle (Resource.String.MainDrawerReportsMonth);
                } else {
                    SupportActionBar.SetTitle (Resource.String.MainDrawerReportsYear);
                }

                OpenFragment (reportFragment.Value);
            } else if (id == DrawerListAdapter.FeedbackPageId) {
                SupportActionBar.SetTitle (Resource.String.MainDrawerFeedback);
                OpenFragment (feedbackFragment.Value);
            } else if (id == DrawerListAdapter.RegisterUserPageId) {
                SupportActionBar.SetTitle (Resource.String.MainDrawerSignup);
                OpenFragment (registerUserFragment.Value);
            } else {
                SupportActionBar.SetTitle (Resource.String.MainDrawerTimer);
                OpenFragment (trackingFragment.Value);
            }
            SetMenuSelection (drawerAdapter.GetItemPosition (id));

            pageStack.Remove (id);
            pageStack.Add (id);
            // Make sure we don't store the timer page as the first page (this is implied)
            if (pageStack.Count == 1 && id == DrawerListAdapter.TimerPageId) {
                pageStack.Clear ();
            }
        }

        public void ForgetCurrentUser ()
        {
            authManager.Forget ();
            StartAuthActivity ();
        }

        public async void ShowSoftKeyboard (View input, bool selectText)
        {
            await System.Threading.Tasks.Task.Delay (100);
            ((InputMethodManager)GetSystemService (InputMethodService)).ShowSoftInput (input, ShowFlags.Implicit);
        }

        public async void HideSoftKeyboard (View input, bool selectText)
        {
            await System.Threading.Tasks.Task.Delay (100);
            ((InputMethodManager)GetSystemService (InputMethodService)).HideSoftInputFromWindow (input.WindowToken, HideSoftInputFlags.ImplicitOnly);
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

        private void OpenFragment (Fragment fragment)
        {
            var old = FragmentManager.FindFragmentById (Resource.Id.ContentFrameLayout);

            if (old == null) {
                FragmentManager.BeginTransaction ()
                .Add (Resource.Id.ContentFrameLayout, fragment)
                .AddToBackStack (fragment.Tag)
                .Commit ();
            } else if (old != fragment) {
                // The detach/attach is a workaround for https://code.google.com/p/android/issues/detail?id=42601
                FragmentManager.BeginTransaction ()
                .Detach (old)
                .Replace (Resource.Id.ContentFrameLayout, fragment)
                .AddToBackStack (fragment.Tag)
                .Attach (fragment)
                .Commit ();
            }
        }

        private void OnDrawerListViewItemClick (object sender, ListView.ItemClickEventArgs e)
        {
            // If tap outside options just close drawer
            if (e.Id == -1) {
                DrawerLayout.CloseDrawers ();
                return;
            }

            // Configure timer component for selected page:
            if (e.Id != DrawerListAdapter.TimerPageId) {
                ToolbarMode = MainDrawerActivity.ToolbarModes.Normal;
            } else {
                ToolbarMode = MainDrawerActivity.ToolbarModes.Timer;
            }

            if (e.Id == DrawerListAdapter.TimerPageId) {
                OpenPage (DrawerListAdapter.TimerPageId);
            } else if (e.Id == DrawerListAdapter.LogoutPageId) {
                ForgetCurrentUser();
            } else if (e.Id == DrawerListAdapter.ReportsPageId) {
                OpenPage (DrawerListAdapter.ReportsPageId);
            } else if (e.Id == DrawerListAdapter.SettingsPageId) {
                OpenPage (DrawerListAdapter.SettingsPageId);
            } else if (e.Id == DrawerListAdapter.FeedbackPageId) {
                OpenPage (DrawerListAdapter.FeedbackPageId);
            } else if (e.Id == DrawerListAdapter.RegisterUserPageId) {
                OpenPage (DrawerListAdapter.RegisterUserPageId);
            }


            DrawerLayout.CloseDrawers ();
        }

        public TimerComponent Timer
        {
            get { return barTimer; }
        }

        public enum ToolbarModes {
            Normal,
            Timer,
            SubView
        }
    }
}
