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

        private void LoadGroup (TimeEntryData data = null, ITimeEntryHolder previous = null)
        {
            var prev = previous as TimeEntryGroup;
            Group = previous != null
                    ? prev.Group.Append (data).OrderBy (x => x.StartTime).ToList ()
            : new List<TimeEntryData> () { data };
        }

        public TimeEntryGroup (TimeEntryData data = null, ITimeEntryHolder previous = null)
        {
            if (data != null) {
                LoadGroup (data, previous);
            }
        }

        public async Task LoadAsync (TimeEntryData data, ITimeEntryHolder previous)
        {
            LoadGroup (data, previous);
            Info = await TimeEntryInfo.LoadAsync (Data);
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            if (object.ReferenceEquals (this, other)) {
                return DiffComparison.Same;
            } else {
                var other2 = other as TimeEntryGroup;
                return other2 != null && other2.Group.First ().Id == Group.First ().Id
                       ? DiffComparison.Updated : DiffComparison.Different;
            }
        }

        public bool Matches (TimeEntryData data)
        {
            return Group.Any (x => x.IsGroupableWith (data));
        }

        public DateTime GetStartTime()
        {
            return Group.FirstOrDefault ().StartTime;
        }

        public TimeSpan GetDuration ()
        {
            TimeSpan duration = TimeSpan.Zero;
            foreach (var item in Group) {
                duration += TimeEntryModel.GetDuration (item, Time.UtcNow);
            }
            return duration;
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
    }
}
