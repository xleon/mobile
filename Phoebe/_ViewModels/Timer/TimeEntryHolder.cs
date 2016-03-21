using System;
using System.Collections.Generic;
using Toggl.Phoebe._Data.Diff;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;

namespace Toggl.Phoebe._ViewModels.Timer
{
    public class TimeEntryHolder : ITimeEntryHolder
    {
        public class Grouper : IGrouper<TimeEntryHolder, TimeEntryHolder>
        {
            public IEnumerable<TimeEntryHolder> Group (IEnumerable<TimeEntryHolder> items)
            {
                return items;
            }
            public IEnumerable<TimeEntryHolder> Ungroup (IEnumerable<TimeEntryHolder> groups)
            {
                return groups;
            }
        }

        public TimeEntryInfo Info { get; set; } // TODO: Make set private again?
        public RichTimeEntry Entry { get; private set; }
        public IList<RichTimeEntry> EntryCollection
        {
            get {
                return new[] { Entry };
            }
        }

        public IList<string> Guids
        {
            get {
                return new List<string> { Entry.Data.Id.ToString() };
            }
        }

        public TimeEntryHolder (RichTimeEntry data)
        {
            Entry = data;
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            if (ReferenceEquals (this, other)) {
                return DiffComparison.Same;
            } else {
                var other2 = other as TimeEntryHolder;
                return other2 != null && other2.Entry.Data.Id == Entry.Data.Id
                       ? DiffComparison.Update : DiffComparison.Different;
            }
        }

        public DateTime GetStartTime()
        {
            return Entry.Data.StartTime;
        }

        public TimeSpan GetDuration()
        {
            return Entry.Data.GetDuration ();
        }

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}, Id={1}]", Entry.Data.StartTime, Entry.Data.Id);
        }
    }
}
