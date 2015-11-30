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

        protected override IList<IHolder> CreateItemCollection (IList<ITimeHolder> holders)
        {
            return holders
                   .Cast<TimeEntryHolder>()
                   .GroupBy (x => x.TimeEntryData.StartTime.ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList ();
        }

        protected override async Task<ITimeHolder> CreateTimeHolder (TimeEntryData entry, ITimeHolder previousHolder = null)
        {
            // Ignore previousHolder
            var holder = new TimeEntryHolder (new List<TimeEntryData>() { entry });
            await holder.LoadAsync();
            return holder;
        }
    }
}
