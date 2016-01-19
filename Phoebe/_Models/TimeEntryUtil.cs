using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;

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

    public class TimeEntryJsonMsg
    {
        public bool HasMore { get; private set; }
        public List<TimeEntryJson> Messages { get; private set; }

        public TimeEntryJsonMsg (bool hasMore, List<TimeEntryJson> messages)
        {
            HasMore = hasMore;
            Messages = messages;
        }
    }

    public class TimeEntryMsg
    {
        public IList<Tuple<TimeEntryData, DataAction>> Messages { get; private set; }

        public bool HasMore {
            get { return Messages.Any (); }
        }

        public TimeEntryMsg (IList<Tuple<TimeEntryData, DataAction>> messages)
        {
            Messages = messages;
        }

        public TimeEntryMsg (TimeEntryData entry, DataAction action)
        {
            Messages = new [] { Tuple.Create (entry, action) };
        }
    }
}

