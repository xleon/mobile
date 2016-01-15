using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Models
{
    // Empty interface just to hide references to IDiffComparable
    public interface IHolder : IDiffComparable
    {
    }

    public interface ITimeEntryHolder : IHolder
    {
        TimeEntryData Data { get; }
        IList<TimeEntryData> DataCollection { get; }
        TimeEntryInfo Info { get; }
        IList<string> Guids { get; }

        TimeSpan GetDuration ();
        DateTime GetStartTime ();
    }

    public enum TimeEntryGroupMethod {
        Single,
        ByDateAndTask
    }

    public class TimeEntryGrouper
    {
        readonly TimeEntryGroupMethod Method;

        public TimeEntryGrouper (TimeEntryGroupMethod method)
        {
            Method = method;
        }

        public IEnumerable<ITimeEntryHolder> Group (IEnumerable<TimeEntryHolder> items)
        {
            return Method == TimeEntryGroupMethod.Single
                   ? items.Cast<ITimeEntryHolder> () : TimeEntryGroup.Group (items);
        }

        public IEnumerable<TimeEntryHolder> Ungroup (IEnumerable<ITimeEntryHolder> groups)
        {
            return Method == TimeEntryGroupMethod.Single
                   ? groups.Cast<TimeEntryHolder> () : TimeEntryGroup.Ungroup (groups.Cast<TimeEntryGroup> ());
        }
    }

    public class TimeEntryLoad
    {
        public Tuple<TimeEntryData, DataAction>[] Messages { get; private set; }
        public DateTime EndDate { get; private set; }

        public TimeEntryLoad (Tuple<TimeEntryData, DataAction>[] messages, DateTime endDate)
        {
            Messages = messages;
            EndDate = endDate;
        }

        public static TimeEntryLoad Aggregate (IEnumerable<TimeEntryLoad> items)
        {
            var endDate = DateTime.MinValue;
            var msgs = new List<Tuple<TimeEntryData, DataAction>> ();
            foreach (var item in items) {
                msgs.AddRange (item.Messages);
                endDate = item.EndDate > endDate ? item.EndDate : endDate;
            }
            return new TimeEntryLoad (msgs.ToArray(), endDate);
        }
    }
}

