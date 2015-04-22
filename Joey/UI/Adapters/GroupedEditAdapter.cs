using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Utils;
using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Joey.UI.Adapters
{
    public class GroupedEditAdapter : RecyclerView.Adapter
    {
        private readonly TimeEntryGroup entryGroup;

        public GroupedEditAdapter (TimeEntryGroup entryGroup)
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


            public ViewHolder (View v) : base (v)
            {
                color = v.FindViewById (Resource.Id.GroupedEditTimeEntryItemTimeColorView);
                period = (TextView)v.FindViewById (Resource.Id.GroupedEditTimeEntryItemTimePeriodTextView);
                duration = (TextView)v.FindViewById (Resource.Id.GroupedEditTimeEntryItemDurationTextView);
            }
        }

        // Create new views (invoked by the layout manager)
        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            View v = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.GroupedEditTimeEntryItem, parent, false);
            var vh = new ViewHolder (v);
            return vh;
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as ViewHolder;
            var entry = entryGroup.TimeEntryList [position];

            if (entryGroup.Model.Project != null) {
                var color = Color.ParseColor (entryGroup.Model.Project.GetHexColor ());
                vh.ColorView.SetBackgroundColor (color);
            }

            var stopTime = (entry.StopTime != null) ? " – " + entry.StopTime.Value.ToShortTimeString () : "";
            vh.PeriodTextView.Text = entry.StartTime.ToShortTimeString () + stopTime;
            vh.DurationTextView.Text = GetDuration (entry, Time.UtcNow).ToString (@"hh\:mm\:ss");
        }

        // Return the size of your dataset (invoked by the layout manager)
        public override int ItemCount
        {
            get { return  entryGroup.TimeEntryList.Count; }
        }

        private static TimeSpan GetDuration (TimeEntryData data, DateTime now)
        {
            if (data.StartTime == DateTime.MinValue) {
                return TimeSpan.Zero;
            }

            var duration = (data.StopTime ?? now) - data.StartTime;
            if (duration < TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }
            return duration;
        }


    }
}

