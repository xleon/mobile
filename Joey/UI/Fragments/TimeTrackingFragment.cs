using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
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
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Full);
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);
            outState.PutInt (ExtraPage, viewPager.CurrentItem);
        }

        public override void OnStart ()
        {
            base.OnStart ();

            ToggleTimerDuration ();

            // Trigger a partial sync, if the sync from OnCreate is still running, it does nothing
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Auto);
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
            if (msg.Model.State == TimeEntryState.Running) {
                viewPager.CurrentItem = MainPagerAdapter.EditPosition;
            } else if (msg.Model.State == TimeEntryState.Finished
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
            private TimeEntryModel currentTimeEntry;
            #pragma warning disable 0414
            private readonly Subscription<ModelChangedMessage> subscriptionModelChanged;
            private readonly Subscription<WelcomeMessageDisabledMessage> subscriptionWelcomeMessageDisabled;
            #pragma warning restore 0414

            public readonly EditCurrentTimeEntryFragment EditFragment = new EditCurrentTimeEntryFragment ();
            public readonly RecentTimeEntriesListFragment RecentFragment = new RecentTimeEntriesListFragment ();
            public readonly LogTimeEntriesListFragment LogFragment = new LogTimeEntriesListFragment ();

            public MainPagerAdapter (Context ctx, FragmentManager fm) : base (fm)
            {
                this.ctx = ctx;

                currentTimeEntry = TimeEntryModel.FindRunning () ?? TimeEntryModel.GetDraft ();

                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
                subscriptionWelcomeMessageDisabled = bus.Subscribe<WelcomeMessageDisabledMessage> (OnWelcomeMessageDisabled);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (msg.Model == currentTimeEntry) {
                    // Listen for changes regarding current running entry
                    if (msg.PropertyName == TimeEntryModel.PropertyState
                        || msg.PropertyName == TimeEntryModel.PropertyStopTime
                        || msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                        if (currentTimeEntry.State == TimeEntryState.Finished || currentTimeEntry.DeletedAt.HasValue) {
                            currentTimeEntry = TimeEntryModel.GetDraft ();
                        }
                        NotifyDataSetChanged ();
                    }
                } else if (msg.Model is TimeEntryModel) {
                    // When some other time entry becomes IsRunning we need to switch over to that
                    if (msg.PropertyName == TimeEntryModel.PropertyState
                        || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                        var entry = (TimeEntryModel)msg.Model;
                        if (entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                            currentTimeEntry = entry;
                            NotifyDataSetChanged ();
                        }
                    }
                }
            }

            private void OnWelcomeMessageDisabled (WelcomeMessageDisabledMessage msg)
            {
                NotifyDataSetChanged ();
            }

            public override int Count {
                get { return PagesCount; }
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted (int position)
            {
                var res = ctx.Resources;

                switch (position) {
                case EditPosition:
                    if (currentTimeEntry != null) {
                        if (currentTimeEntry.State == TimeEntryState.Running) {
                            return res.GetTextFormatted (Resource.String.TimeTrackingRunningTab);
                        } else if (currentTimeEntry.State == TimeEntryState.New && currentTimeEntry.StopTime.HasValue) {
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

            private static bool ForCurrentUser (TimeEntryModel model)
            {
                var authManager = ServiceContainer.Resolve<AuthManager> ();
                return model.UserId == authManager.UserId;
            }
        }
    }
}

