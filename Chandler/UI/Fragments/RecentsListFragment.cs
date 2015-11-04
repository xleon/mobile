using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.Wearable.Views;
using Android.Views;
using Toggl.Chandler.UI.Adapters;
using Android.Widget;

namespace Toggl.Chandler.UI.Fragments
{
    public class RecentsListFragment : Fragment, WearableListView.IClickListener, WearableListView.IOnScrollListener
    {
        private readonly List<SimpleTimeEntryData> dataItems = new List<SimpleTimeEntryData> ();
        private WearableListView listView;
        private TextView headerTextView;

        public RecentsListFragment (List<SimpleTimeEntryData> data)
        {
            dataItems = data;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ListFragment, container, false);
            headerTextView = view.FindViewById<TextView> (Resource.Id.ListTitleTextView);

            listView = view.FindViewById<WearableListView> (Resource.Id.RecentTimeEntriesList);
            listView.SetAdapter (new RecentListAdapter (Activity, dataItems));
            listView.SetGreedyTouchMode (true);
            listView.AddOnScrollListener (this);
            listView.SetClickListener (this);

            return view;
        }

        public void OnClick (WearableListView.ViewHolder v)
        {
            var tag = v.ItemView.Tag;
            //Start this TE;
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
