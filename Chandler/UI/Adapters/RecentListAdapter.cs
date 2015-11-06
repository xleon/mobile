using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.Wearable.Views;
using Android.Views;
using Android.Widget;
using Toggl.Chandler.UI.Activities;

namespace Toggl.Chandler.UI.Adapters
{
    public class RecentListAdapter : WearableListView.Adapter
    {
        private List<SimpleTimeEntryData> data = new List<SimpleTimeEntryData> ();
        private LayoutInflater Inflater;
        private MainActivity Activity;
        private Context Context;
        public RecentListAdapter (Context ctx, Activity activity)
        {
            Inflater = LayoutInflater.FromContext (ctx);
            Activity = (MainActivity)activity;
            Context = ctx;

            Activity.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged (object sender, EventArgs e)
        {
            data = Activity.Data;
            NotifyDataSetChanged();
        }

        public class ItemViewHolder : WearableListView.ViewHolder
        {
            public TextView DescriptionTextView;
            public TextView ProjectTextView;
            public View ColorView;


            public ItemViewHolder (View view) : base (view)
            {
                DescriptionTextView = (TextView) view.FindViewById (Resource.Id.RecentListDescription);
                ProjectTextView = (TextView) view.FindViewById (Resource.Id.RecentListProject);
                ColorView = (View) view.FindViewById (Resource.Id.ColorView);
            }

            public void Bind (SimpleTimeEntryData data, Context ctx)
            {
                DescriptionTextView.Text = String.IsNullOrEmpty (data.Description) ? ctx.Resources.GetString (Resource.String.TimeEntryNoDescription) : data.Description;
                ProjectTextView.Text = String.IsNullOrEmpty (data.Project) ? ctx.Resources.GetString (Resource.String.TimeEntryNoProject) : data.Project;
                var color = Color.ParseColor (data.ProjectColor);
                var shape = ColorView.Background as GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }
            }
        }

        #region implemented abstract members of Adapter

        public override void OnBindViewHolder (Android.Support.V7.Widget.RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = (ItemViewHolder) holder;
            viewHolder.Bind (data[position], Context);
            holder.ItemView.Tag = data[position].Id.ToString();
        }

        public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder (Android.Views.ViewGroup parent, int viewType)
        {
            return new ItemViewHolder (Inflater.Inflate (Resource.Layout.RecentListItem, null));
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
