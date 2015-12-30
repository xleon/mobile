using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe.Models
{
    /// <summary>
    // Wrapper to manage groups of TimeEntry objects
    /// </summary>
    public class TimeEntryGroup : ITimeEntryHolder
    {
        public TimeEntryInfo Info { get; private set; }
        public List<TimeEntry> Group { get; private set; }

        public TimeEntry Data
        {
            get { return Group[0]; }
        }

        public IList<string> Guids
        {
            get {
                return Group.AsEnumerable ().Select (r => r.Id.ToString ()).ToList ();
            }
        }

        TimeEntryGroup () { }

        public TimeEntryGroup (TimeEntry data, ITimeEntryHolder previous = null)
        {
            var previous2 = previous as TimeEntryGroup;
            if (previous2 != null) {
                Group = previous2.Group.ReplaceOrAppend (data, x => x.Id == data.Id)
                        .OrderByDescending (x => x.StartTime).ToList ();

                // Recycle entry info if possible
                Info = previous2.Data.Id == Data.Id ? previous2.Info : null;
            } else {
                Group = new List<TimeEntry> { data };
            }
        }

        public ITimeEntryHolder UpdateOrDelete (TimeEntry data, out bool isAffectedByDelete)
        {
            isAffectedByDelete = Group.Any (x => x.Id == data.Id);

            if (isAffectedByDelete) {
                if (Group.Count == 1) {
                    return null; // Delete
                } else {
                    var updated = new TimeEntryGroup ();
                    updated.Group = Group.Where (x => x.Id != data.Id).ToList ();
                    updated.Info = updated.Data.Id == Data.Id ? Info : null; // Recycle entry info if possible
                    return updated;
                }
            } else {
                return null;
            }
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

        public bool IsAffectedByPut (TimeEntry data)
        {
            return Group.Any (x => x.IsGroupableWith (data));
        }

        public DateTime GetStartTime()
        {
            return Data.StartTime;
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
            return string.Format ("[{0:MM/dd HH:mm}]", GetStartTime ());
        }
    }
}
