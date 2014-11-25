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
using System.Collections.Generic;
using Toggl.Phoebe;
using XPlatUtils;
using Toggl.Phoebe.Net;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsPagerFragment : Fragment
    {
        private static readonly int PagesCount = 2000;
        private ViewPager viewPager;
        private ImageButton previousPeriod;
        private ImageButton nextPeriod;
        private TextView timePeriod;
        private int backDate;
        public ZoomLevel zoomLevel = ZoomLevel.Week;
        private List<string> dates = new List<string> ();

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsPagerFragment, container, false);
            viewPager = view.FindViewById<ViewPager> (Resource.Id.ReportsViewPager);
            viewPager.PageScrolled += OnViewPagerPageScrolled;

            timePeriod = view.FindViewById<TextView> (Resource.Id.TimePeriodLabel);
            previousPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonPrevious);
            nextPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonNext);

            previousPeriod.Click += (sender, e) => NavigatePage (-1);
            nextPeriod.Click += (sender, e) => NavigatePage (1);

            return view;
        }

        public void NavigatePage (int direction)
        {
            viewPager.SetCurrentItem (viewPager.CurrentItem + direction, true);
            backDate = viewPager.CurrentItem + direction - PagesCount / 2;
            UpdatePeriod ();
        }

        public override void OnDestroyView ()
        {
            viewPager.PageScrolled -= OnViewPagerPageScrolled;
            base.OnDestroyView ();
        }

        public override void OnActivityCreated (Bundle savedInstanceState)
        {
            base.OnActivityCreated (savedInstanceState);
            viewPager.Adapter = new MainPagerAdapter (ChildFragmentManager);
            viewPager.CurrentItem = PagesCount / 2;
            Initialize ();
        }

        public void Initialize ()
        {
            timePeriod.Text = FormattedDateSelector ();
        }

        private void UpdatePeriod ()
        {
            timePeriod.Text = FormattedDateSelector ();
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
                backDate = viewPager.CurrentItem - PagesCount / 2;
                UpdatePeriod ();
            }
        }

        public string FormattedDateSelector ()
        {
            if (backDate == 0) {
                if (zoomLevel == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsThisWeek);
                } else if (zoomLevel == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsThisMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsThisYear);
                }
            } else if (backDate == -1) {
                if (zoomLevel == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsLastWeek);
                } else if (zoomLevel == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsLastMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsLastYear);
                }
            } else {
                var startDate = ResolveStartDate (backDate);

                if (zoomLevel == ZoomLevel.Week) {
                    var endDate = ResolveEndDate (startDate);
                    return String.Format ("{0:MMM dd}th - {1:MMM dd}th", startDate, endDate);
                } else if (zoomLevel == ZoomLevel.Month) {
                    return String.Format ("{0:M}", startDate);
                }
                return startDate.Year.ToString ();
            }
        }

        public DateTime ResolveStartDate (int backDate)
        {
            var current = DateTime.Today;
            if (zoomLevel == ZoomLevel.Week) {
                var user = ServiceContainer.Resolve<AuthManager> ().User;
                var startOfWeek = user.StartOfWeek;
                var date = current.StartOfWeek (startOfWeek).AddDays (backDate * 7);
                return date;
            }

            if (zoomLevel == ZoomLevel.Month) {
                current = current.AddMonths (backDate);
                return new DateTime (current.Year, current.Month, 1);
            }

            return new DateTime (current.Year + backDate, 1, 1);
        }

        public DateTime ResolveEndDate (DateTime start)
        {
            if (zoomLevel == ZoomLevel.Week) {
                return start.AddDays (6);
            }

            if (zoomLevel == ZoomLevel.Month) {
                return start.AddMonths (1).AddDays (-1);
            }

            return start.AddYears (1).AddDays (-1);
        }

        private class MainPagerAdapter : FragmentPagerAdapter
        {
            public int Current = PagesCount / 2;
            private ZoomLevel zoomLevel = ZoomLevel.Week;

            public MainPagerAdapter (FragmentManager fm) : base (fm)
            {
            }

            public override int Count {
                get { return PagesCount; }
            }

            public override Fragment GetItem (int position)
            {
                return new ReportsFragment (position - PagesCount / 2);
            }
        }
    }
}
