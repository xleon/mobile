using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Android.Graphics;

using Toggl.Phoebe.Data.Utils;

namespace Toggl.Joey.UI.Adapters
{
    public class GroupTimeEntriesAdapter : RecyclerView.Adapter
    {
        public const string TAG = "CustomAdapter";
        private TimeEntryGroup entryGroup;

        public GroupTimeEntriesAdapter (TimeEntryGroup entryGroup)
        {
            this.entryGroup = entryGroup;
            entryGroup.InitModel ();
        }

        // Provide a reference to the type of views that you are using (custom ViewHolder)
        public class ViewHolder : RecyclerView.ViewHolder
        {
            private View color;
            private TextView period;
            private TextView duration;
          
            public View ColorView
            {
                get { return color; }
            }

            public TextView PeriodTextView 
            {
                get { return period; }
            }

            public TextView DurationTextView
            {
                get { return duration; }
            }


            public ViewHolder(View v) : base(v)
            {
                color = v.FindViewById(Resource.Id.GroupedEditTimeEntryItemTimeColorView);
                period = (TextView)v.FindViewById(Resource.Id.GroupedEditTimeEntryItemTimePeriodTextView);
                duration = (TextView)v.FindViewById(Resource.Id.GroupedEditTimeEntryItemDurationTextView);
            }
        }

        // Create new views (invoked by the layout manager)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup viewGroup, int position)
        {
            View v = LayoutInflater.From (viewGroup.Context).Inflate (Resource.Layout.GroupedEditTimeEntryItem, viewGroup, false);
            ViewHolder vh = new ViewHolder (v);
            return vh;
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var vh = viewHolder as ViewHolder;
            var entry = entryGroup.TimeEntryList [position];

            if (entryGroup.Model.Project != null) {
                var color = Color.ParseColor (entryGroup.Model.Project.GetHexColor ());
                vh.ColorView.SetBackgroundColor (color);
            }

            vh.PeriodTextView.SetText (entry.StartTime.ToShortTimeString () + " – " + entry.StopTime.Value.ToShortTimeString (), TextView.BufferType.Normal);
            vh.DurationTextView.SetText ("00:50", TextView.BufferType.Normal);
        }

        // Return the size of your dataset (invoked by the layout manager)
        public override int ItemCount
        {
            get{ return  entryGroup.TimeEntryList.Count; }
        }


    }
}

