using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Utils
{
    /// <summary>
    // Wrapper to manage groups of TimeEntryData objects
    /// </summary>
    public class TimeEntryGroup : ITimeEntryHolder
    {
        public class Grouper : IGrouper<TimeEntryHolder, TimeEntryGroup>
        {
            public IEnumerable<TimeEntryGroup> Group (IEnumerable<TimeEntryHolder> items)
            {
                var key = default (Guid);
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
            public IEnumerable<TimeEntryHolder> Disgroup (IEnumerable<TimeEntryGroup> groups)
            {
                foreach (var g in groups) {
                    foreach (var data in g.DataCollection) {
                        yield return new TimeEntryHolder (data, g.Info); // Cache TimeEntryInfo
                    }
                }
            }
        }

        public TimeEntryInfo Info { get; private set; }
        public List<TimeEntryData> DataCollection { get; private set; }

        public TimeEntryData Data
        {
            get { return DataCollection [0]; }
        }

        public IList<string> Guids
        {
            get {
                return DataCollection.AsEnumerable ().Select (r => r.Id.ToString ()).ToList ();
            }
        }

        public TimeEntryGroup (TimeEntryData data)
        {
            DataCollection = new List<TimeEntryData> { data };
        }

        public TimeEntryGroup (IEnumerable<TimeEntryData> dataCollection, TimeEntryInfo info)
        {
            DataCollection = dataCollection.OrderByDescending (x => x.StartTime).ToList ();
            Info = info;
        }

        public async Task LoadInfoAsync ()
        {
            Info = Info ?? await TimeEntryInfo.LoadAsync (Data);
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            var other2 = other as TimeEntryGroup;
            if (other2 != null) {
                if (DataCollection.SequenceEqual (other2.DataCollection, object.ReferenceEquals)) {
                    return DiffComparison.Same;
                } else {
                    return other2.DataCollection.Last ().Id == DataCollection.Last ().Id
                           ? DiffComparison.Update : DiffComparison.Different;
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
            return DataCollection.Aggregate (TimeSpan.Zero, (acc, x) => acc + TimeEntryModel.GetDuration (x, Time.UtcNow));
        }

        public async Task DeleteAsync ()
        {
            var deleteTasks = new List<Task> ();
            foreach (var item in DataCollection) {
                var m = new TimeEntryModel (item);
                deleteTasks.Add (m.DeleteAsync ());
            }
            await Task.WhenAll (deleteTasks);
        }

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}]", GetStartTime ());
        }
    }
}
