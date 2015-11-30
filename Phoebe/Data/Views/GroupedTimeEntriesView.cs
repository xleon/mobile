using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// </summary>
    public class GroupedTimeEntriesView : TimeEntriesCollectionView
    {
        public GroupedTimeEntriesView ()
        {
            Tag = "GroupedTimeEntriesView";
        }

        protected override IList<IHolder> CreateItemCollection (IList<ITimeHolder> holders)
        {
            return holders
                   .Cast<TimeEntryGroup> ()
                   .GroupBy (x => x.StartTime.ToLocalTime ().Date)
                   .SelectMany (gr => gr.Cast<IHolder> ().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList();
        }

        protected override async Task<ITimeHolder> CreateTimeHolder (TimeEntryData entry, ITimeHolder previousHolder = null)
        {
            var holder = previousHolder as TimeEntryGroup;
            if (holder != null) {
                holder = new TimeEntryGroup (holder.TimeEntryList);
                holder.Add (entry);
            } else {
                holder = new TimeEntryGroup (entry);
            }
            await holder.LoadAsync();
            return holder;
        }
    }
}
