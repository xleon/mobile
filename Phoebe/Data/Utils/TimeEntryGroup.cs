using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using PropertyChanged;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using TTask = System.Threading.Tasks.Task;

namespace Toggl.Phoebe.Data.Utils
{
    /// <summary>
    // Wrapper to manage groups of TimeEntryData objects
    /// </summary>
    [DoNotNotify]
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

        public TimeEntryGroup (TimeEntryData data)
        {
            Group = new List<TimeEntryData> () { data };
        }

        public TimeEntryGroup (IEnumerable<TimeEntryData> dataList)
        {
            Group = dataList.OrderBy (x => x.StartTime).ToList ();
        }

        public void Dispose ()
        {
            Group.Clear ();
            Info = null;
        }

        public bool Equals (IHolder obj)
        {
            var other = obj as TimeEntryGroup;
            return other != null && other.Data.Id == Data.Id;
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

        public async Task LoadAsync ()
        {
            Info = await TimeEntryInfo.LoadAsync (Data);
        }

        public async Task DeleteAsync ()
        {
            var deleteTasks = new List<Task> ();
            foreach (var item in Group) {
                var m = new TimeEntryModel (item);
                deleteTasks.Add (m.DeleteAsync ());
            }
            await TTask.WhenAll (deleteTasks);
            Dispose ();
        }
    }
}
