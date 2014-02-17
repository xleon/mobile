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

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true)]
    public class TimeTrackingActivity : BaseActivity
    {
        private static readonly int PagesCount = 2;
        private ViewPager viewPager;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);
            SetContentView (Resource.Layout.TimeTrackingViewPager);
            viewPager = FindViewById<ViewPager> (Resource.Id.ViewPager);

            var adapter = new MainPagerAdapter (this, SupportFragmentManager);
            viewPager.Adapter = adapter;
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

