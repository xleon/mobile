using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentPagerAdapter = Android.Support.V4.App.FragmentPagerAdapter;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using ViewPager = Android.Support.V4.View.ViewPager;

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

        private ZoomLevel zoomPeriod;

        public ZoomLevel ZoomLevel {
            get {
                return zoomPeriod;
            }
            set {
                zoomPeriod = value;
                UpdatePager ();
            }
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsPagerFragment, container, false);
            viewPager = view.FindViewById<ViewPager> (Resource.Id.ReportsViewPager);
            viewPager.PageSelected += OnPageSelected;

            timePeriod = view.FindViewById<TextView> (Resource.Id.TimePeriodLabel);
            previousPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonPrevious);
            nextPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonNext);

            previousPeriod.Click += (sender, e) => NavigatePage (-1);
            nextPeriod.Click += (sender, e) => NavigatePage (1);

            return view;
        }

        public override void OnDestroyView ()
        {
            viewPager.PageSelected -= OnPageSelected;
            base.OnDestroyView ();
        }

        public void NavigatePage (int direction)
        {
            viewPager.SetCurrentItem (viewPager.CurrentItem + direction, true);
            backDate = viewPager.CurrentItem + direction - PagesCount / 2;
            UpdatePeriod ();
        }
            
        public override void OnResume ()
        {
            base.OnResume ();
            viewPager.Adapter = new MainPagerAdapter (ChildFragmentManager);
            viewPager.CurrentItem = PagesCount / 2;
            timePeriod.Text = FormattedDateSelector ();
        }

        private void UpdatePager ()
        {
            var adapter = (MainPagerAdapter)viewPager.Adapter;
            adapter.ZoomLevel = zoomPeriod;
            adapter.ClearFragmentList ();
            adapter.NotifyDataSetChanged ();
            UpdatePeriod ();
        }

        private void UpdatePeriod ()
        {
            timePeriod.Text = FormattedDateSelector ();
        }

        private async void OnPageSelected ( object sender, ViewPager.PageSelectedEventArgs e)
        {
            var adapter = (MainPagerAdapter)viewPager.Adapter;
            adapter.ZoomLevel = zoomPeriod;

            var frag = (ReportsFragment)adapter.GetItem ( e.Position);
            if (frag.IsResumed) {
                frag.LoadElements ();
                frag.UserVisibleHint = true;
            } else {
                await Task.Delay (200);
                OnPageSelected (sender, e); // recursive?
            }
            backDate = e.Position - PagesCount / 2;
            UpdatePeriod ();
        }

        private string FormattedDateSelector ()
        {
            if (backDate == 0) {
                if (ZoomLevel == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsThisWeek);
                } else if (ZoomLevel == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsThisMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsThisYear);
                }
            } else if (backDate == -1) {
                if (ZoomLevel == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsLastWeek);
                } else if (ZoomLevel == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsLastMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsLastYear);
                }
            } else {
                var startDate = ResolveStartDate (backDate);
                if (ZoomLevel == ZoomLevel.Week) {
                    var endDate = ResolveEndDate (startDate);
                    return String.Format ("{0:MMM dd}th - {1:MMM dd}th", startDate, endDate);
                } else if (ZoomLevel == ZoomLevel.Month) {
                    return String.Format ("{0:M}", startDate);
                }
                return startDate.Year.ToString ();
            }
        }

        private DateTime ResolveStartDate (int back)
        {
            var current = DateTime.Today;

            if (ZoomLevel == ZoomLevel.Week) {
                var user = ServiceContainer.Resolve<AuthManager> ().User;
                var startOfWeek = user.StartOfWeek;
                var date = current.StartOfWeek (startOfWeek).AddDays (back * 7);
                return date;
            }

            if (ZoomLevel == ZoomLevel.Month) {
                current = current.AddMonths (back);
                return new DateTime (current.Year, current.Month, 1);
            }

            return new DateTime (current.Year + back, 1, 1);
        }

        private DateTime ResolveEndDate (DateTime start)
        {
            if (ZoomLevel == ZoomLevel.Week) {
                return start.AddDays (6);
            }

            if (ZoomLevel == ZoomLevel.Month) {
                return start.AddMonths (1).AddDays (-1);
            }

            return start.AddYears (1).AddDays (-1);
        }

        private class MainPagerAdapter : FragmentPagerAdapter
        {
            public int Current = PagesCount / 2;

            private List<ReportsFragment> fragmentList;

            private ChartPosition lastPosition;

            public List<ReportsFragment> FragmentList
            {
                get { return fragmentList; }
            }

            private ZoomLevel zoomLevel = ZoomLevel.Week;

            private FragmentManager fragmentManager;

            public ZoomLevel ZoomLevel {
                get {
                    return zoomLevel;
                }
                set {
                    zoomLevel = value;
                }
            }

            public MainPagerAdapter (FragmentManager fm) : base (fm)
            {
                fragmentList = new List<ReportsFragment>();
                fragmentManager = fm;
            }

            public override int Count {
                get { return PagesCount; }
            }
                            
            public override Java.Lang.Object InstantiateItem (ViewGroup container, int position)
            {
                var obj =  (ReportsFragment)base.InstantiateItem (container, position);
                fragmentList.Add (obj);
                obj.Position = lastPosition;
                obj.PositionChanged += ChangeReportsPosition;
                return obj;
            }

            public void ClearFragmentList ()
            {
                fragmentManager.Fragments.Clear ();
            }

            public override void DestroyItem (ViewGroup container, int position, Java.Lang.Object @object)
            {
                var obj = (ReportsFragment)@object;
                fragmentList.Remove (obj);
                obj.PositionChanged -= ChangeReportsPosition;
                base.DestroyItem (container, position, @object);
            }

            public override Fragment GetItem (int position)
            {
                var item = FragmentList.Find (r => r.Period == (position - PagesCount / 2));
                var result =  item ?? new ReportsFragment ((position - PagesCount / 2), zoomLevel);
                result.Position = lastPosition;
                return result;
            }

            private void ChangeReportsPosition ( object sender, EventArgs args )
            {
                var pos = ((ReportsFragment)sender).Position;
                foreach (var item in FragmentList) {
                    item.Position = pos;
                }
                lastPosition = pos;
            }
        }
    }
}
