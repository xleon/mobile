using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe.Data.Utils
{
    public class DateHolder : IHolder
    {
        public DateTime Date { get; }
        public bool IsRunning { get; private set; }
        public TimeSpan TotalDuration { get; private set; }

        public DateHolder (DateTime date, IEnumerable<ITimeEntryHolder> timeHolders = null)
        {
            var totalDuration = TimeSpan.Zero;
            var dataObjects = timeHolders != null ? timeHolders.ToList () : new List<ITimeEntryHolder> ();
            foreach (var item in dataObjects) {
                totalDuration += item.GetDuration ();
            }

            Date = date;
            TotalDuration = totalDuration;
            IsRunning = dataObjects.Any (g => g.Data.State == TimeEntryState.Running);
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            var other2 = other as DateHolder;
            if (other2 == null || other2.Date != Date) {
                return DiffComparison.Different;
            } else {
                var same = other2.TotalDuration == TotalDuration && other2.IsRunning == IsRunning;
                return same ? DiffComparison.Same : DiffComparison.SoftUpdate;
            }
        }

        public override string ToString ()
        {
            return string.Format ("Date {0:dd/MM}", Date);
        }
    }
}
