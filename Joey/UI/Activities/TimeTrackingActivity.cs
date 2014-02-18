using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FragmentPagerAdapter = Android.Support.V4.App.FragmentPagerAdapter;
using ActionBar = Android.Support.V7.App.ActionBar;
using ViewPager = Android.Support.V4.View.ViewPager;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using Toggl.Joey.UI.Fragments;
using Android.Graphics.Drawables;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true,
        Theme = "@style/Theme.Toggl.App")]
    public class TimeTrackingActivity : BaseActivity
    {
        private static readonly string SelectedNavIndexExtra = "com.toggl.android.navigation_index";
        private static readonly int PagesCount = 2;

        private ViewPager viewPager;
        private TimerFragment timerFragment = new TimerFragment ();
  
        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.TimeTrackingViewPager);

            var adapter = new MainPagerAdapter (this, SupportFragmentManager);
            viewPager = FindViewById<ViewPager> (Resource.Id.ViewPager);
            viewPager.Adapter = adapter;

            var lp = new ActionBar.LayoutParams (ActionBar.LayoutParams.WrapContent, ActionBar.LayoutParams.WrapContent);
            lp.Gravity = (int) (GravityFlags.Right | GravityFlags.CenterVertical);
            View customNav = LayoutInflater.From (this).Inflate (Resource.Layout.TimerFragment, null); // layout which contains your button.
            ActionBar.SetCustomView (customNav, lp);
            ActionBar.SetDisplayShowCustomEnabled (true);

            if (bundle != null) {
                viewPager.SetCurrentItem (bundle.GetInt (SelectedNavIndexExtra), false);
            }

            timerFragment.OnCreate (customNav, this);

            // Make sure that the user will see newest data when they start the activity
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Full);
        }

        protected override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);

            outState.PutInt (SelectedNavIndexExtra, viewPager.CurrentItem);
        }

        protected override void OnStart ()
        {
            base.OnStart ();
            timerFragment.OnStart ();

            // Trigger a partial sync, if the sync from OnCreate is still running, it does nothing
            ServiceContainer.Resolve<SyncManager> ().Run (SyncMode.Auto);
        }

        protected override void OnStop ()
        {
            base.OnStop ();
            timerFragment.OnStop ();
        }

        private class MainPagerAdapter : FragmentPagerAdapter {
            private Context ctx;

            public MainPagerAdapter(Context ctx, FragmentManager fm) : base(fm) {
                this.ctx = ctx;
            }

            public override int Count {
                get {return PagesCount;}
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted (int position)
            {
                var names = ctx.Resources.GetStringArray (Resource.Array.TimeEntriesNavigationList);
                if(position >= names.Length)
                  throw new InvalidOperationException ("Unknown tab position");

                return new Java.Lang.String(names [position].ToUpper());
            }

            public override Fragment GetItem(int position) {
                Fragment fragment;
                switch (position) {
                case 0:
                    fragment = new RecentTimeEntriesListFragment ();
                    break;
                case 1:
                    fragment = new LogTimeEntriesListFragment ();
                    break;
                default:
                    throw new InvalidOperationException ("Unknown tab position");
                }

                return fragment;
            }
        }
    }
}

