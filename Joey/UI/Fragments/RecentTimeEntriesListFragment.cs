using System;
using System.Linq;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class RecentTimeEntriesListFragment : ListFragment
    {
        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            var headerView = new View (Activity);
            int headerWidth = (int) TypedValue.ApplyDimension (ComplexUnitType.Dip, 6, Resources.DisplayMetrics);
            headerView.SetMinimumHeight (headerWidth);
            ListView.AddHeaderView (headerView);
            ListAdapter = new RecentTimeEntriesAdapter ();
        }

        public override void OnDestroyView ()
        {
            base.OnDestroyView ();
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            RecentTimeEntriesAdapter adapter = null;
            if (l.Adapter is HeaderViewListAdapter) {
                var headerAdapter = (HeaderViewListAdapter)l.Adapter;
                adapter = headerAdapter.WrappedAdapter as RecentTimeEntriesAdapter;
                // Adjust the position by taking into account the fact that we've got headers
                position -= headerAdapter.HeadersCount;
            } else if (l.Adapter is RecentTimeEntriesAdapter) {
                adapter = (RecentTimeEntriesAdapter)l.Adapter;
            }

            if (adapter == null || position < 0 || position >= adapter.Count)
                return;

            var model = adapter.GetModel (position);
            if (model == null)
                return;

            model.Continue ();
        }
    }
}
