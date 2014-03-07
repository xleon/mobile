using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class RecentTimeEntriesListFragment : ListFragment
    {
        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTitleTextView)
                .SetFont (Font.Roboto)
                .SetText (Resource.String.RecentTimeEntryNoItemsTitle);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView)
                .SetFont (Font.RobotoLight);
            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ListView.SetClipToPadding (false);
            ListAdapter = new RecentTimeEntriesAdapter ();
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
