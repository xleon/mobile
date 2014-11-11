using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentPagerAdapter = Android.Support.V4.App.FragmentPagerAdapter;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using ViewPager = Android.Support.V4.View.ViewPager;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsPagerFragment : Fragment
    {
        private static readonly int PagesCount = 50;
        private ViewPager viewPager;
        private int currentBackDate;
        private ZoomLevel period;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsPagerFragment, container, false);
            viewPager = view.FindViewById<ViewPager> (Resource.Id.ReportsViewPager);
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
            viewPager.CurrentItem = (int)MainPagerAdapter.Current;
        }



        private void OnViewPagerPageScrolled (object sender, ViewPager.PageScrolledEventArgs e)
        {
            var current = viewPager.CurrentItem;
            var pos = e.Position + e.PositionOffset;
            int idx;
            if (pos + 0.05f < current) {
                idx = (int)Math.Floor (pos);
            } else if (pos - 0.05f > current) {
                idx = (int)Math.Ceiling (pos);
            } else {
                return;
            }

            var adapter = (MainPagerAdapter)viewPager.Adapter;
            if (adapter != null) {
                var frag = (ReportsFragment)adapter.GetItem (idx);
                frag.UserVisibleHint = true;
            }

        }

        private void OnViewPagerPageSelected (object sender, ViewPager.PageSelectedEventArgs e)
        {
        }

        private class MainPagerAdapter : FragmentPagerAdapter
        {
            private Context ctx;
            public const int Previous = -1;
            public const int Current = 25;
            public const int Next = 1;
            public int currentBackDate;

            public MainPagerAdapter (Context ctx, FragmentManager fm) : base (fm)
            {
                this.ctx = ctx;
                currentBackDate = 0;
            }

            public override int Count {
                get { return PagesCount; }
            }

            public override Fragment GetItem (int position)
            {
                return new ReportsFragment (25 - position);
            }
        }
    }
}
