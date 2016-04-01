using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Diff;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;

namespace Toggl.Phoebe.ViewModels.Timer
{
    public class TimeEntryHolder : ITimeEntryHolder
    {
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
            var other2 = other as TimeEntryHolder;
            if (other2 != null) {
                if (Entry.Data.Id == other2.Entry.Data.Id) {
                    return Entry.Equals (other2.Entry) ? DiffComparison.Same : DiffComparison.Update;
                } else {
                    return DiffComparison.Different;
                }
            } else {
                return DiffComparison.Different;
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
