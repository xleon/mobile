using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public class TimeEntryTagCollectionView : ICollectionDataView<TagData>
    {
        private readonly Guid timeEntryId;
        private readonly HashSet<Guid> tagIds = new HashSet<Guid> ();
        private List<TagData> tagsList = new List<TagData> ();
        private List<string> tagNames = new List<string> ();

        TimeEntryTagCollectionView (Guid timeEntryId)
        {
            this.timeEntryId = timeEntryId;
        }

        public static async Task<TimeEntryTagCollectionView> Init (Guid timeEntryId)
        {
            var v = new TimeEntryTagCollectionView (timeEntryId);
            v.tagIds.Clear ();
            v.tagsList.Clear ();
            v.OnUpdated ();

            try {
                var store = ServiceContainer.Resolve<IDataStore> ();
                v.tagsList = await store.GetTimeEntryTags (timeEntryId);
                foreach (var tag in v.tagsList) {
                    v.tagIds.Add (tag.Id);
                }
            } finally {
                v.OnUpdated ();
            }

            return v;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Dispose ()
        {
        }

        private void SortTags ()
        {
            tagsList.Sort ((a, b) => {
                var aName = a != null ? (a.Name ?? String.Empty) : String.Empty;
                var bName = b != null ? (b.Name ?? String.Empty) : String.Empty;
                return String.Compare (aName, bName, StringComparison.Ordinal);
            });
        }

        public IEnumerable<TagData> Data
        {
            get { return tagsList; }
        }

        public List<string> TagNames
        {
            get { return tagNames; }
        }

        public int Count
        {
            get { return tagNames.Count; }
        }

        public bool HasNonDefault
        {
            get {
                return tagNames.FirstOrDefault (t => t != TimeEntryModel.DefaultTag) != null;
            }
        }

        public bool HasMore
        {
            get { return false; }
        }

        public void LoadMore ()
        {
            // Do nothing
        }

        private void OnUpdated ()
        {
            SortTags ();

            // Update tag names list
            tagNames.Clear ();
            tagNames.AddRange (tagsList
                               .Where (t => tagIds.Contains (t.Id))
                               .Select (t => t.Name));

            // Notify listeners
            var handler = CollectionChanged;
            if (handler != null) {
                handler (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
            }
        }

        private async Task LoadTagData (Guid id)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var rows = await store.Table<TagData> ()
                       .Where (r => r.Id == id && r.DeletedAt == null)
                       .ToListAsync();
            var tag = rows.FirstOrDefault ();

            if (tag != null && tagIds.Contains (tag.Id)) {
                if (!tagsList.UpdateData (tag)) {
                    tagsList.Add (tag);
                }
                OnUpdated ();
            }
        }
    }
}

