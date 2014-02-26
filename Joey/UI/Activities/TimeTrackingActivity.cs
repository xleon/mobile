using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Fragments;
using ActionBar = Android.Support.V7.App.ActionBar;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentPagerAdapter = Android.Support.V4.App.FragmentPagerAdapter;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using ViewPager = Android.Support.V4.View.ViewPager;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true,
        WindowSoftInputMode = SoftInput.AdjustPan,
        Theme = "@style/Theme.Toggl.App")]
    public class TimeTrackingActivity : BaseActivity
    {
        private static readonly int PagesCount = 3;
        private ViewPager viewPager;
        private TimerComponent timerSection = new TimerComponent ();

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.TimeTrackingActivity);

            var adapter = new MainPagerAdapter (this, SupportFragmentManager);
            viewPager = FindViewById<ViewPager> (Resource.Id.ViewPager);
            viewPager.Adapter = adapter;
            viewPager.CurrentItem = MainPagerAdapter.RecentPosition;

            timerSection.OnCreate (this);

            var lp = new ActionBar.LayoutParams (ActionBar.LayoutParams.WrapContent, ActionBar.LayoutParams.WrapContent);
            lp.Gravity = (int) (GravityFlags.Right | GravityFlags.CenterVertical);

            ActionBar.SetCustomView (timerSection.Root, lp);
            ActionBar.SetDisplayShowCustomEnabled (true);

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Full);
        }

        protected override void OnStart ()
        {
            base.OnStart ();
            timerSection.OnStart ();

            // Trigger a partial sync, if the sync from OnCreate is still running, it does nothing
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Auto);
        }

        protected override void OnStop ()
        {
            base.OnStop ();
            timerSection.OnStop ();
        }

        private class MainPagerAdapter : FragmentPagerAdapter
        {
            public const int EditPosition = 0;
            public const int RecentPosition = 1;
            public const int LogPosition = 2;
            private Context ctx;
            private readonly CurrentTimeEntryEditFragment editFragment = new CurrentTimeEntryEditFragment ();
            private readonly RecentTimeEntriesListFragment recentFragment = new RecentTimeEntriesListFragment ();
            private readonly LogTimeEntriesListFragment logFragment = new LogTimeEntriesListFragment ();

            public MainPagerAdapter (Context ctx, FragmentManager fm) : base (fm)
            {
                this.ctx = ctx;
            }

            public override int Count {
                get { return PagesCount; }
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted (int position)
            {
                var res = ctx.Resources;

                switch (position) {
                case EditPosition:
                    // TODO: Determine if there are any running time entries
                    var isRunning = false;

                    if (isRunning) {
                        return res.GetTextFormatted (Resource.String.TimeTrackingRunningTab);
                    } else {
                        return res.GetTextFormatted (Resource.String.TimeTrackingNewTab);
                    }
                case RecentPosition:
                    // TODO: Determine if first run:
                    var firstRun = false;

                    if (firstRun) {
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
                    return editFragment;
                case RecentPosition:
                    return recentFragment;
                case LogPosition:
                    return logFragment;
                default:
                    throw new InvalidOperationException ("Unknown tab position");
                }
            }
        }
    }
}

