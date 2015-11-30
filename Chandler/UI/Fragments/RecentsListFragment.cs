using Android.App;
using Android.OS;
using Android.Support.Wearable.Views;
using Android.Views;
using Android.Widget;
using Toggl.Chandler.UI.Activities;
using Toggl.Chandler.UI.Adapters;

namespace Toggl.Chandler.UI.Fragments
{
    public class RecentsListFragment : Fragment, WearableListView.IClickListener, WearableListView.IOnScrollListener
    {
        private WearableListView listView;
        private TextView headerTextView;
        private RecentListAdapter listAdapter;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ListFragment, container, false);
            headerTextView = view.FindViewById<TextView> (Resource.Id.ListTitleTextView);

            listView = view.FindViewById<WearableListView> (Resource.Id.RecentTimeEntriesList);
            listAdapter = new RecentListAdapter (Activity, Activity);
            listView.SetAdapter (listAdapter);
            listView.SetGreedyTouchMode (true);
            listView.AddOnScrollListener (this);
            listView.SetClickListener (this);
            return view;
        }

        public void OnClick (WearableListView.ViewHolder v)
        {
            var tag = v.ItemView.Tag;
            ((MainActivity)Activity).RequestStartEntry (tag.ToString());
        }

        public void OnTopEmptyRegionClick ()
        {
        }

        public void OnAbsoluteScrollChange (int i)
        {
            if (i > 0) {
                headerTextView.SetY (-i);
            }
        }

        public void OnCentralPositionChanged (int i)
        {
        }

        public void OnScroll (int i)
        {
        }

        public void OnScrollStateChanged (int i)
        {
        }
    }
}
