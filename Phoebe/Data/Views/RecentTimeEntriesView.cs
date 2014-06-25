using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view returns the recent unique time entries.
    /// </summary>
    public class RecentTimeEntriesView : IDataView<TimeEntryData>, IDisposable
    {
        private static readonly string Tag = "RecentTimeEntriesView";
        private DateTime queryStartDate;
        private GroupContainer data = new GroupContainer ();
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;

        public RecentTimeEntriesView ()
        {
            Reload ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionDataChange != null) {
                bus.Unsubscribe (subscriptionDataChange);
                subscriptionDataChange = null;
            }
            if (subscriptionSyncFinished != null) {
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        private void OnDataChange (DataChangeMessage msg)
        {
            var entry = msg.Data as TimeEntryData;
            if (entry == null)
                return;

            var authManager = ServiceContainer.Resolve<AuthManager> ();
            var isExcluded = msg.Action == DataAction.Delete
                             || entry.DeletedAt.HasValue
                             || entry.State == TimeEntryState.New
                             || entry.StartTime < queryStartDate
                             || entry.UserId != authManager.GetUserId ();

            Group grp;
            TimeEntryData existingEntry;
            if (data.Find (entry, out grp, out existingEntry)) {
                if (isExcluded) {
                    grp.Remove (entry);
                    if (grp.IsEmpty) {
                        data.Remove (grp);
                    } else {
                        data.Sort ();
                    }
                    OnUpdated ();
                } else {
                    var groupChanged = !grp.CanContain (entry);
                    var startChanged = existingEntry.StartTime != entry.StartTime;

                    if (groupChanged) {
                        // Remove from old group:
                        grp.Remove (entry);
                        if (grp.IsEmpty) {
                            data.Remove (grp);
                        }

                        // Add entry to correct group
                        data.Add (entry);
                        data.Sort ();
                        OnUpdated ();
                    } else if (startChanged) {
                        // Update group and resort data:
                        grp.Update (entry);
                        data.Sort ();
                        OnUpdated ();
                    }
                }
            } else if (!isExcluded) {
                data.Add (entry);
                data.Sort ();
                OnUpdated ();
            }
        }

        public event EventHandler Updated;

        private void OnUpdated ()
        {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public async void Reload ()
        {
            if (IsLoading)
                return;

            var store = ServiceContainer.Resolve<IDataStore> ();
            var bus = ServiceContainer.Resolve<MessageBus> ();
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
            var shouldSubscribe = false;

            if (subscriptionDataChange != null) {
                shouldSubscribe = true;
                bus.Unsubscribe (subscriptionDataChange);
                subscriptionDataChange = null;
            }

            data.Clear ();
            IsLoading = true;
            OnUpdated ();

            // Group only items in the past 9 days
            queryStartDate = Time.UtcNow - TimeSpan.FromDays (9);
            var query = store.Table<TimeEntryData> ()
                .OrderBy (r => r.StartTime, false)
                .Where (r => r.DeletedAt == null
                        && r.UserId == userId
                        && r.State != TimeEntryState.New
                        && r.StartTime >= queryStartDate);

            // Get new data
            data = await FromQuery (query);
            if (shouldSubscribe) {
                subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
            }

            // Determine if sync is running, if so, delay setting IsLoading to false
            var syncManager = ServiceContainer.Resolve<SyncManager> ();
            if (!syncManager.IsRunning) {
                IsLoading = false;
            } else {
                subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
            }
            OnUpdated ();
        }

        public void LoadMore ()
        {
        }

        public IEnumerable<TimeEntryData> Data {
            get { return data.Where ((g) => !g.IsEmpty).Select ((g) => g.First ()); }
        }

        public long Count {
            get { return data.Count ((g) => !g.IsEmpty); }
        }

        public bool HasMore {
            get { return false; }
        }

        public bool IsLoading { get; private set; }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            IsLoading = false;
            OnUpdated ();

            if (subscriptionSyncFinished != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        private static async Task<GroupContainer> FromQuery (IDataQuery<TimeEntryData> query)
        {
            var groups = new GroupContainer ();

            try {
                var entries = await query.QueryAsync ().ConfigureAwait (false);

                // Find unique time entries and add them to the list
                foreach (var entry in entries) {
                    if (groups.Contains (entry)) {
                        continue;
                    }
                    groups.Add (entry);
                }

                groups.Sort ();
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to compose recent time entries");
            }
            return groups;
        }

        private class GroupContainer : IEnumerable<Group>
        {
            readonly List<Group> data = new List<Group> ();

            public void Sort ()
            {
                data.Sort ((a, b) => b.RecentStartTime.CompareTo (a.RecentStartTime));
            }

            public bool Find (TimeEntryData entry, out Group group, out TimeEntryData existingEntry)
            {
                foreach (var grp in data) {
                    foreach (var d in grp) {
                        if (entry.Matches (d)) {
                            group = grp;
                            existingEntry = d;
                            return true;
                        }
                    }
                }

                group = null;
                existingEntry = null;
                return false;
            }

            public bool Contains (TimeEntryData entry)
            {
                TimeEntryData existing;
                Group grp;
                return Find (entry, out grp, out existing);
            }

            public void Add (TimeEntryData entry)
            {
                var grp = data.FirstOrDefault ((g) => g.CanContain (entry));
                if (grp == null) {
                    grp = new Group (entry);
                    data.Add (grp);
                } else {
                    grp.Add (entry);
                }
            }

            public void Clear ()
            {
                data.Clear ();
            }

            public bool Remove (Group item)
            {
                return data.Remove (item);
            }

            public IEnumerator<Group> GetEnumerator ()
            {
                return data.GetEnumerator ();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
            {
                return GetEnumerator ();
            }

            public int Count {
                get { return data.Count; }
            }
        }

        private class Group : IEnumerable<TimeEntryData>
        {
            private readonly string description;
            private readonly Guid? taskId;
            private readonly Guid? projectId;
            private readonly List<TimeEntryData> items = new List<TimeEntryData> ();

            public Group (TimeEntryData entry)
            {
                description = entry.Description;
                taskId = entry.TaskId;
                projectId = entry.ProjectId;

                Add (entry);
            }

            public DateTime RecentStartTime {
                get {
                    if (items.Count < 1)
                        return DateTime.MinValue;
                    return items [0].StartTime;
                }
            }

            public bool IsEmpty {
                get { return items.Count < 1; }
            }

            public bool CanContain (TimeEntryData entry)
            {
                // Check data:
                if (description != entry.Description
                    || taskId != entry.TaskId
                    || projectId != entry.ProjectId)
                    return false;

                return true;
            }

            public void Add (TimeEntryData entry)
            {
                items.Add (entry);
                items.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
            }

            public void Update (TimeEntryData entry)
            {
                items.UpdateData (entry);
                items.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
            }

            public void Remove (TimeEntryData entry)
            {
                items.RemoveAll (item => item.Matches (entry));
            }

            public bool Contains (TimeEntryData entry)
            {
                return items.Any (item => item.Matches (entry));
            }

            public IEnumerator<TimeEntryData> GetEnumerator ()
            {
                return items.GetEnumerator ();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
            {
                return GetEnumerator ();
            }
        }
    }
}
