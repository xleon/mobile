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

        protected override IList<IHolder> CreateItemCollection (IList<ITimeEntryHolder> timeHolders)
        {
            return timeHolders
                   .Cast<TimeEntryGroup> ()
                   .GroupBy (x => x.GetStartTime ().ToLocalTime ().Date)
                   .SelectMany (gr => gr.Cast<IHolder> ().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList();
        }

        protected override async Task<ITimeEntryHolder> CreateTimeHolder (TimeEntryData entry, ITimeEntryHolder previousHolder = null)
        {
            return await TimeEntryGroup.LoadAsync (entry, previousHolder as TimeEntryGroup);
        }
    }
}
