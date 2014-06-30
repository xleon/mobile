using System;
using System.ComponentModel;
using Android.Content;
using Android.OS;
using Android.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentPagerAdapter = Android.Support.V4.App.FragmentPagerAdapter;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using ViewPager = Android.Support.V4.View.ViewPager;

namespace Toggl.Joey.UI.Fragments
{
    public class TimeTrackingFragment : Fragment
    {
        private static readonly int PagesCount = 3;
        private static readonly string ExtraPage = "com.toggl.timer.page";
        private ViewPager viewPager;
        private Subscription<UserTimeEntryStateChangeMessage> subscriptionUserTimeEntryStateChange;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeTrackingFragment, container, false);
            viewPager = view.FindViewById<ViewPager> (Resource.Id.ViewPager);
            viewPager.PageScrolled += OnViewPagerPageScrolled;
            viewPager.PageSelected += OnViewPagerPageSelected;
            return view;
        }

        public override void OnDestroyView ()
        {
            viewPager.PageSelected -= OnViewPagerPageSelected;
            viewPager.PageScrolled -= OnViewPagerPageScrolled;
            base.OnDestroyView ();
        }

        public override void OnActivityCreated (Bundle savedInstanceState)
        {
            base.OnActivityCreated (savedInstanceState);

            viewPager.Adapter = new MainPagerAdapter (Activity, ChildFragmentManager);

            if (savedInstanceState != null) {
                viewPager.CurrentItem = savedInstanceState.GetInt (ExtraPage, (int)MainPagerAdapter.RecentPosition);
            } else {
                viewPager.CurrentItem = (int)MainPagerAdapter.RecentPosition;
            }

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<ISyncManager> ().Run (SyncMode.Full);
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);
            if (viewPager != null) {
                outState.PutInt (ExtraPage, viewPager.CurrentItem);
            }
        }

        public override void OnStart ()
        {
            base.OnStart ();

            ToggleTimerDuration ();

            // Trigger a partial sync, if the sync from OnCreate is still running, it does nothing
            ServiceContainer.Resolve<ISyncManager> ().Run (SyncMode.Auto);
        }

        public override void OnResume ()
        {
            base.OnResume ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionUserTimeEntryStateChange = bus.Subscribe<UserTimeEntryStateChangeMessage> (OnUserTimeEntryStateChange);
        }

        public override void OnPause ()
        {
            if (subscriptionUserTimeEntryStateChange != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionUserTimeEntryStateChange);
                subscriptionUserTimeEntryStateChange = null;
            }

            base.OnPause ();
        }

        private void OnViewPagerPageScrolled (object sender, ViewPager.PageScrolledEventArgs e)
        {
            var current = viewPager.CurrentItem;
            var pos = e.Position + e.PositionOffset;
            int idx;
            if (pos + 0.05f < current) {
                // Moving to the left
                idx = (int)Math.Floor (pos);
            } else if (pos - 0.05f > current) {
                // Moving to the right
                idx = (int)Math.Ceiling (pos);
            } else {
                return;
            }

            // Make sure the fragment knows that it's about to become visible
            var adapter = (MainPagerAdapter)viewPager.Adapter;
            if (adapter != null) {
                var frag = adapter.GetItem (idx);
                frag.UserVisibleHint = true;
            }
        }

        private void OnViewPagerPageSelected (object sender, ViewPager.PageSelectedEventArgs e)
        {
            if (e.Position != MainPagerAdapter.LogPosition) {
                ((MainPagerAdapter)viewPager.Adapter).LogFragment.CloseActionMode ();
            }

            ToggleTimerDuration ();
        }

        private void OnUserTimeEntryStateChange (UserTimeEntryStateChangeMessage msg)
        {
            if (msg.Data.State == TimeEntryState.Running) {
                viewPager.CurrentItem = MainPagerAdapter.EditPosition;
            } else if (msg.Data.State == TimeEntryState.Finished
                       && viewPager.CurrentItem == MainPagerAdapter.EditPosition) {
                viewPager.CurrentItem = MainPagerAdapter.RecentPosition;
            }
        }

        private void ToggleTimerDuration ()
        {
            var timer = Timer;
            if (timer != null) {
                timer.HideDuration = viewPager.CurrentItem == MainPagerAdapter.EditPosition;
            }
        }

        private TimerComponent Timer {
            get {
                var activity = Activity as MainDrawerActivity;
                if (activity != null) {
                    return activity.Timer;
                }
                return null;
            }
        }

        private class MainPagerAdapter : FragmentPagerAdapter
        {
            public const int EditPosition = 0;
            public const int RecentPosition = 1;
            public const int LogPosition = 2;
            private Context ctx;
            private ActiveTimeEntryManager timeEntryManager;
            private readonly Subscription<SettingChangedMessage> subscriptionSettingChanged;

            public readonly EditCurrentTimeEntryFragment EditFragment = new EditCurrentTimeEntryFragment ();
            public readonly RecentTimeEntriesListFragment RecentFragment = new RecentTimeEntriesListFragment ();
            public readonly LogTimeEntriesListFragment LogFragment = new LogTimeEntriesListFragment ();

            public MainPagerAdapter (Context ctx, FragmentManager fm) : base (fm)
            {
                this.ctx = ctx;

                timeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
                timeEntryManager.PropertyChanged += OnTimeEntryManagerPropertyChanged;

                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (OnSettingChanged);
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    if (subscriptionSettingChanged != null) {
                        bus.Unsubscribe (subscriptionSettingChanged);
                        subscriptionSettingChanged = null;
                    }

                    if (timeEntryManager != null) {
                        timeEntryManager.PropertyChanged -= OnTimeEntryManagerPropertyChanged;
                        timeEntryManager = null;
                    }
                }

                base.Dispose (disposing);
            }

            private void OnTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (args.PropertyName == ActiveTimeEntryManager.PropertyActive) {
                    NotifyDataSetChanged ();
                }
            }

            private void OnSettingChanged (SettingChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (msg.Name == SettingsStore.PropertyGotWelcomeMessage) {
                    NotifyDataSetChanged ();
                }
            }

            public override int Count {
                get { return PagesCount; }
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted (int position)
            {
                var res = ctx.Resources;

                switch (position) {
                case EditPosition:
                    if (timeEntryManager != null && timeEntryManager.Active != null) {
                        if (timeEntryManager.Active.State == TimeEntryState.Running) {
                            return res.GetTextFormatted (Resource.String.TimeTrackingRunningTab);
                        } else if (timeEntryManager.Active.State == TimeEntryState.New && timeEntryManager.Active.StopTime.HasValue) {
                            return res.GetTextFormatted (Resource.String.TimeTrackingManualTab);
                        }
                    }
                    return res.GetTextFormatted (Resource.String.TimeTrackingNewTab);
                case RecentPosition:
                    var settings = ServiceContainer.Resolve<SettingsStore> ();
                    if (!settings.GotWelcomeMessage) {
                        return res.GetTextFormatted (Resource.String.TimeTrackingWelcomeTab);
                    } else {
                        return res.GetTextFormatted (Resource.String.TimeTrackingRecentTab);
                    }
                case LogPosition:
                    return res.GetTextFormatted (Resource.String.TimeTrackingLogTab);
                default:
                    throw new InvalidOperationException ("Unknown tab position");
                }
            }

            public override Fragment GetItem (int position)
            {
                switch (position) {
                case EditPosition:
                    return EditFragment;
                case RecentPosition:
                    return RecentFragment;
                case LogPosition:
                    return LogFragment;
                default:
                    throw new InvalidOperationException ("Unknown tab position");
                }
            }
        }
    }
}
