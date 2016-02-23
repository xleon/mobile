using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Diff;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;

namespace Toggl.Phoebe._ViewModels.Timer
{
    /// <summary>
    // Wrapper to manage groups of TimeEntry objects
    /// </summary>
    public class TimeEntryGroup : ITimeEntryHolder
    {
        public static IEnumerable<TimeEntryGroup> Group (IEnumerable<TimeEntryHolder> items)
        {
            Guid key;
            var tempDic = new Dictionary<Guid, List<TimeEntryHolder>> ();
            foreach (var item in items) {
                if (tempDic.TryFindKey (out key, kv => kv.Value[0].Data.IsGroupableWith (item.Data))) {
                    tempDic [key].Add (item);
                } else {
                    tempDic.Add (item.Data.Id, new List<TimeEntryHolder> { item });
                }
            }
            foreach (var kvPair in tempDic) {
                var cached = kvPair.Value.FirstOrDefault (x => x.Info != null);
                yield return new TimeEntryGroup (kvPair.Value.Select (x => x.Data), cached != null ? cached.Info : null);
            }
        }
        public static IEnumerable<TimeEntryHolder> Ungroup (IEnumerable<TimeEntryGroup> groups)
        {
            foreach (var g in groups) {
                foreach (var data in g.DataCollection) {
                    yield return new TimeEntryHolder (data, g.Info); // Cache TimeEntryInfo
                }
            }
        }

        public TimeEntryInfo Info { get; private set; }
        public IList<ITimeEntryData> DataCollection { get; private set; }

        public ITimeEntryData Data
        {
            get { return DataCollection [0]; }
        }

        public IList<string> Guids
        {
            get {
                return DataCollection.AsEnumerable ().Select (r => r.Id.ToString ()).ToList ();
            }
        }

        public TimeEntryGroup (ITimeEntryData data)
        {
            DataCollection = new List<ITimeEntryData> { data };
        }

        public TimeEntryGroup (IEnumerable<ITimeEntryData> dataCollection, TimeEntryInfo info)
        {
            DataCollection = dataCollection.OrderByDescending (x => x.StartTime).ToList ();
            Info = info;
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            var other2 = other as TimeEntryGroup;
            if (other2 != null) {
                if (DataCollection.SequenceEqual (other2.DataCollection, object.ReferenceEquals)) {
                    return DiffComparison.Same;
                } else {
                    return Data.IsGroupableWith (other2.Data) ?
                           DiffComparison.Update : DiffComparison.Different;
                }
            } else {
                return DiffComparison.Different;
            }
        }

        public DateTime GetStartTime()
        {
            return Data.StartTime;
        }

        public TimeSpan GetDuration ()
        {
            return DataCollection.Aggregate (TimeSpan.Zero, (acc, x) => acc + x.GetDuration ());
        }

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}]", GetStartTime ());
        }
    }
}