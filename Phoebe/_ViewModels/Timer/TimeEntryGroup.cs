using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe._Data.Diff;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;

namespace Toggl.Phoebe._ViewModels.Timer
{
    /// <summary>
    // Wrapper to manage groups of TimeEntry objects
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
                    if (tempDic.TryFindKey (out key, kv => kv.Value [0].Entry.Data.IsGroupableWith (item.Entry.Data))) {
                        tempDic [key].Add (item);
                    } else {
                        tempDic.Add (item.Entry.Data.Id, new List<TimeEntryHolder> { item });
                    }
                }
                foreach (var kvPair in tempDic) {
                    yield return new TimeEntryGroup (kvPair.Value.Select (x => x.Entry));
                }
            }
            public IEnumerable<TimeEntryHolder> Ungroup (IEnumerable<TimeEntryGroup> groups)
            {
                foreach (var g in groups) {
                    foreach (var data in g.EntryCollection) {
                        yield return new TimeEntryHolder (data);
                    }
                }
            }
        }

        public IList<RichTimeEntry> EntryCollection { get; private set; }

        public RichTimeEntry Entry
        {
            get { return EntryCollection [0]; }
        }

        public IList<string> Guids
        {
            get {
                return EntryCollection.AsEnumerable ().Select (r => r.Data.Id.ToString ()).ToList ();
            }
        }

        public TimeEntryGroup (RichTimeEntry entry)
        {
            EntryCollection = new List<RichTimeEntry> { entry };
        }

        public TimeEntryGroup (IEnumerable<RichTimeEntry> entryCollection)
        {
            EntryCollection = entryCollection.OrderByDescending (x => x.Data.StartTime).ToList ();
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            var other2 = other as TimeEntryGroup;
            if (other2 != null) {
                if (EntryCollection.SequenceEqual (other2.EntryCollection, ReferenceEquals)) {
                    return DiffComparison.Same;
                } else {
                    return Entry.Data.IsGroupableWith (other2.Entry.Data) ?
                           DiffComparison.Update : DiffComparison.Different;
                }
            } else {
                return DiffComparison.Different;
            }
        }

        public DateTime GetStartTime()
        {
            return Entry.Data.StartTime;
        }

        public TimeSpan GetDuration ()
        {
            return EntryCollection.Aggregate (TimeSpan.Zero, (acc, x) => acc + x.Data.GetDuration ());
        }

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}]", GetStartTime ());
        }
    }
}