using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Views
{
    public class TimeEntryTagsView : IDataView<string>
    {
        private readonly Guid timeEntryId;
        private readonly HashSet<Guid> tagIds = new HashSet<Guid> ();
        private List<TagData> tagsList = new List<TagData> ();
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private List<string> tagNames = new List<string> ();

        public TimeEntryTagsView (Guid timeEntryId)
        {
            this.timeEntryId = timeEntryId;

            Reload ();
        }

        public event EventHandler Updated;

        private void OnUpdated ()
        {
            SortTags ();

            // Update tag names list
            tagNames.Clear ();
            tagNames.AddRange (tagsList
                .Where (t => tagIds.Contains (t.Id))
                .Select (t => t.Name));

            // Notify listeners
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void OnDataChange (DataChangeMessage msg)
        {
            if (msg.Data is TagData) {
                OnDataChange ((TagData)msg.Data, msg.Action);
            } else if (msg.Data is TimeEntryTagData) {
                OnDataChange ((TimeEntryTagData)msg.Data, msg.Action);
            }
        }

        private void OnDataChange (TagData data, DataAction action)
        {
            var shouldUpdate = false;
            var isExcluded = action == DataAction.Delete
                             || data.DeletedAt != null
                             || !tagIds.Contains (data.Id);

            if (isExcluded) {
                var removed = tagsList.RemoveAll (d => d.Matches (data));
                shouldUpdate = removed > 0;
            } else {
                shouldUpdate = tagsList.UpdateData (data);
            }

            if (shouldUpdate) {
                OnUpdated ();
            }
        }

        private void OnDataChange (TimeEntryTagData data, DataAction action)
        {
            var shouldUpdate = false;
            var isExcluded = action == DataAction.Delete
                             || data.DeletedAt != null
                             || data.TimeEntryId != timeEntryId;

            if (isExcluded) {
                shouldUpdate = tagIds.Remove (data.TagId);
            } else {
                shouldUpdate = tagIds.Add (data.TagId);
                var shouldAdd = shouldUpdate && !tagsList.Any (i => i.Id == data.TagId);

                if (shouldAdd) {
                    // Try to add TagData from cache, or load it
                    var tag = TagCache.Get (data.TagId);
                    if (tag != null) {
                        tagsList.Add (tag);
                    } else {
                        shouldUpdate = false;
                        LoadTagData (data.TagId);
                    }
                }
            }

            if (shouldUpdate) {
                OnUpdated ();
            }
        }

        private async void LoadTagData (Guid id)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var rows = await store.Table<TagData> ().QueryAsync (r => r.Id == id && r.DeletedAt == null);
            var tag = rows.FirstOrDefault ();

            if (tag != null && tagIds.Contains (tag.Id)) {
                if (!tagsList.UpdateData (tag)) {
                    tagsList.Add (tag);
                }
                OnUpdated ();
            }
        }

        public async void Reload ()
        {
            if (IsLoading)
                return;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionDataChange != null) {
                bus.Unsubscribe (subscriptionDataChange);
            }

            IsLoading = true;
            tagIds.Clear ();
            tagsList.Clear ();
            OnUpdated ();

            try {
                var store = ServiceContainer.Resolve<IDataStore> ();
                tagsList = await store.GetTimeEntryTags (timeEntryId);
                foreach (var tag in tagsList) {
                    tagIds.Add (tag.Id);
                }
                TagCache.Put (tagsList);
            } finally {
                IsLoading = false;
                OnUpdated ();

                if (subscriptionDataChange == null) {
                    subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
                }
            }
        }

        public void LoadMore ()
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

        public IEnumerable<string> Data {
            get { return tagNames; }
        }

        public long Count {
            get { return tagNames.Count; }
        }

        public bool HasNonDefault {
            get {
                return tagNames.FirstOrDefault (t => t != TimeEntryModel.DefaultTag) != null;
            }
        }

        public bool HasMore {
            get { return false; }
        }

        public bool IsLoading { get; private set; }

        private static readonly WeakReference<WeakCache<TagData>> weakTagCache = new WeakReference<WeakCache<TagData>> (null);

        private static WeakCache<TagData> TagCache {
            get {
                lock (weakTagCache) {
                    WeakCache<TagData> cache;
                    if (!weakTagCache.TryGetTarget (out cache)) {
                        cache = new WeakCache<TagData> ();
                        weakTagCache.SetTarget (cache);
                    }
                    return cache;
                }
            }
        }

        private class WeakCache<T>
            where T : CommonData
        {
            private readonly List<WeakReference> data = new List<WeakReference> ();

            public void Put (T obj)
            {
                var inserted = false;

                lock (data) {
                    // Try to reuse weak reference instances first:
                    foreach (var weak in data) {
                        var item = (T)weak.Target;
                        if (item == null || item.Id == obj.Id) {
                            if (!inserted) {
                                weak.Target = obj;
                                inserted = true;
                            } else {
                                weak.Target = null;
                            }
                        }
                    }

                    if (!inserted) {
                        data.Add (new WeakReference (obj));
                    }
                }
            }

            public void Put (IEnumerable<T> objs)
            {
                lock (data) {
                    foreach (var obj in objs) {
                        Put (obj);
                    }
                }
            }

            public T Get (Guid id)
            {
                lock (data) {
                    return data
                        .Select (weak => (T)weak.Target)
                        .FirstOrDefault (inst => inst != null && inst.Id == id);
                }
            }
        }
    }
}
