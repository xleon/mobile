using System;
using System.Linq;
using Android.OS;
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
            headerView.SetMinimumHeight (6);
            ListView.AddHeaderView (headerView);
            ListAdapter = new RecentTimeEntriesAdapter ();
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            var adapter = l.Adapter as RecentTimeEntriesAdapter;
            if (adapter == null)
                return;

            var model = adapter.GetModel (position);
            if (model == null)
                return;

            model.Continue ();
        }
    }
}
