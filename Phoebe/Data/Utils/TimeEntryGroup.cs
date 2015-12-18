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
        public TimeEntryInfo Info { get; private set; }
        public List<TimeEntryData> Group { get; private set; }

        public TimeEntryData Data
        {
            get { return Group.Last (); }
        }

        public IList<string> Guids
        {
            get {
                return Group.AsEnumerable ().Select (r => r.Id.ToString ()).ToList ();
            }
        }

        public TimeEntryGroup (TimeEntryData data, ITimeEntryHolder previous = null)
        {
            var prev = previous as TimeEntryGroup;
            if (prev != null) {
                Group = prev.Group.ReplaceOrAppend (data, x => x.Id == data.Id).OrderByDescending (x => x.StartTime).ToList ();
            } else {
                Group = new List<TimeEntryData> { data };
            }
        }

        public async Task LoadInfoAsync ()
        {
            Info = await TimeEntryInfo.LoadAsync (Data);
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            if (object.ReferenceEquals (this, other)) {
                return DiffComparison.Same;
            } else {
                var other2 = other as TimeEntryGroup;
                return other2 != null && other2.Group.Last ().Id == Group.Last ().Id
                       ? DiffComparison.Update : DiffComparison.Different;
            }
        }

        public bool Matches (TimeEntryData data)
        {
            return Group.Any (x => x.IsGroupableWith (data));
        }

        public DateTime GetStartTime()
        {
            return Group[0].StartTime;
        }

        public TimeSpan GetDuration ()
        {
            return Group.Aggregate (TimeSpan.Zero, (acc, x) => acc + TimeEntryModel.GetDuration (x, Time.UtcNow));
        }

        public async Task DeleteAsync ()
        {
            var deleteTasks = new List<Task> ();
            foreach (var item in Group) {
                var m = new TimeEntryModel (item);
                deleteTasks.Add (m.DeleteAsync ());
            }
            await Task.WhenAll (deleteTasks);
        }

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}]", Data.StartTime);
        }
    }
}
