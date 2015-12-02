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
    public class LogTimeEntriesView : TimeEntriesCollectionView
    {
        public LogTimeEntriesView ()
        {
            Tag = "LogTimeEntriesView";
        }

        protected override IList<IHolder> CreateItemCollection (IList<ITimeEntryHolder> timeHolders)
        {
            return timeHolders
                   .Cast<TimeEntryHolder>()
                   .GroupBy (x => x.Data.StartTime.ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList ();
        }

        protected override async Task<ITimeEntryHolder> CreateTimeHolder (TimeEntryData entry, ITimeEntryHolder previousHolder = null)
        {
            // Ignore previousHolder
            return await TimeEntryHolder.LoadAsync (entry);
        }
    }
}
