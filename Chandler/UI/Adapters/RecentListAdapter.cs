using Android.Support.Wearable.Views;
using System.Collections.Generic;
using Android.Views;
using Android.Content;
using Android.Widget;
using System;

namespace Toggl.Chandler.UI.Adapters
{
    public class RecentListAdapter : WearableListView.Adapter
    {
        private List<SimpleTimeEntryData> data = new List<SimpleTimeEntryData> ();
        private LayoutInflater inflater;

        public RecentListAdapter (Context ctx, List<SimpleTimeEntryData> dataItems)
        {
            inflater = LayoutInflater.FromContext (ctx);
            data = dataItems;
        }

        public class ItemViewHolder : WearableListView.ViewHolder
        {
            public TextView DescriptionTextView;
            public TextView ProjectTextView;

            public ItemViewHolder (View view) : base (view)
            {
                DescriptionTextView = (TextView) view.FindViewById (Resource.Id.RecentListDescription);
                ProjectTextView = (TextView) view.FindViewById (Resource.Id.RecentListProject);
            }
        }

        #region implemented abstract members of Adapter

        public override void OnBindViewHolder (Android.Support.V7.Widget.RecyclerView.ViewHolder holder, int position)
        {
            var holderItem = (ItemViewHolder) holder;

            holderItem.DescriptionTextView.Text = data[position].Description;
            Console.WriteLine ("desc: {0}, project: {1}", data[position].Description, data[position].Project);
            holderItem.ProjectTextView.Text = data[position].Project;

            holderItem.ItemView.Tag = position;
        }

        public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder (Android.Views.ViewGroup parent, int viewType)
        {
            return new ItemViewHolder (inflater.Inflate (Resource.Layout.RecentListItem, null));
        }

        public override int ItemCount
        {
            get {
                return data.Count;
            }
        }

        #endregion

    }
}

