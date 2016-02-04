using System;
using System.Collections.Generic;
using Toggl.Phoebe._Data.Diff;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;

namespace Toggl.Phoebe._ViewModels.Timer
{
    public class TimeEntryHolder : ITimeEntryHolder
    {
        public TimeEntryInfo Info { get; set; } // TODO: Make set private again?
        public TimeEntryData Data { get; private set; }
        public IList<TimeEntryData> DataCollection
        {
            get {
                return new[] { Data };
            }
        }

        public IList<string> Guids
        {
            get {
                return new List<string> { Data.Id.ToString() };
            }
        }

        public TimeEntryHolder (TimeEntryData data, TimeEntryInfo info)
        {
            Data = data;
            Info = info;
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            if (object.ReferenceEquals (this, other)) {
                return DiffComparison.Same;
            } else {
                var other2 = other as TimeEntryHolder;
                return other2 != null && other2.Data.Id == Data.Id
                       ? DiffComparison.Update : DiffComparison.Different;
            }
        }

        public DateTime GetStartTime()
        {
            return Data.StartTime;
        }

        public TimeSpan GetDuration()
        {
            return Data.GetDuration (Time.UtcNow);
        }

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}, Id={1}]", Data.StartTime, Data.Id);
        }
    }
}
