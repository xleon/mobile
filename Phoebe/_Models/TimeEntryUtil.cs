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

    public class TimeEntryMsg : List<Tuple<DataVerb, TimeEntryData>>, IDataSyncGroup
    {
        public DataDir Dir { get; private set; }
        public Type DataType { get { return typeof(TimeEntryData); } }

        public IEnumerable<DataSyncMsg> SyncMessages {
            get { 
                return this.Select (x => new DataSyncMsg (Dir, x.Item1, x.Item2));;
            }
        }

    public class TimeEntryMsg : List<DataActionMsg<TimeEntryData>>
    {
        public TimeEntryMsg (IEnumerable<DataActionMsg<TimeEntryData>> messages)
        : base (messages)
        {
            Dir = dir;
        }

        public TimeEntryMsg (TimeEntryData entry, DataAction action)
        : base (new [] { new DataActionMsg<TimeEntryData> (entry, action) })
        {
            Dir = dir;
        }
    }
}

