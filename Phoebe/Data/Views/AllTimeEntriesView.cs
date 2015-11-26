using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IDataStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class AllTimeEntriesView : IDataView<object>, IDisposable
    {
        private static readonly string Tag = "AllTimeEntriesView";
        private readonly List<DateGroup> dateGroups = new List<DateGroup> ();
        private UpdateMode updateMode = UpdateMode.Immediate;
        private DateTime startFrom;
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private bool reloadScheduled;

        public AllTimeEntriesView ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);

            HasMore = true;
            Reload ();
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
            if (entry == null) {
                return;
            }

            var isExcluded = entry.DeletedAt != null
                             || msg.Action == DataAction.Delete
                             || entry.State == TimeEntryState.New;

            if (isExcluded) {
                RemoveEntry (entry);
            } else {
                AddOrUpdateEntry (new TimeEntryData (entry));
            }
        }

        private void AddOrUpdateEntry (TimeEntryData entry)
        {
            TimeEntryData existingEntry;
            DateGroup grp;

            if (FindExistingEntry (entry, out grp, out existingEntry)) {
                if (entry.StartTime != existingEntry.StartTime) {
                    var date = entry.StartTime.ToLocalTime ().Date;
                    if (grp.Date != date) {
                        // Need to move entry:
                        grp.Remove (existingEntry);

                        grp = GetGroupFor (entry);
                        grp.Add (entry);
                    } else {
                        grp.DataObjects.UpdateData (entry);
                    }
                    Sort ();
                } else {
                    grp.DataObjects.UpdateData (entry);
                }
                OnUpdated ();
            } else {
                grp = GetGroupFor (entry);
                grp.Add (entry);
                Sort ();
                OnUpdated ();
            }
        }

        private void RemoveEntry (TimeEntryData entry)
        {
            DateGroup grp;
            TimeEntryData oldEntry;
            if (FindExistingEntry (entry, out grp, out oldEntry)) {
                grp.Remove (oldEntry);
                if (grp.DataObjects.Count == 0) {
                    dateGroups.Remove (grp);
                }
                OnUpdated ();
            }
        }

        private bool FindExistingEntry (TimeEntryData dataObject, out DateGroup dateGroup, out TimeEntryData existingDataObject)
        {
            foreach (var grp in dateGroups) {
                foreach (var obj in grp.DataObjects) {
                    if (dataObject.Matches (obj)) {
                        dateGroup = grp;
                        existingDataObject = obj;
                        return true;
                    }
                }
            }

            dateGroup = null;
            existingDataObject = null;
            return false;
        }

        private DateGroup GetGroupFor (TimeEntryData dataObject)
        {
            var date = dataObject.StartTime.ToLocalTime ().Date;
            var grp = dateGroups.FirstOrDefault (g => g.Date == date);
            if (grp == null) {
                grp = new DateGroup (date);
                dateGroups.Add (grp);
            }
            return grp;
        }

        private void Sort ()
        {
            foreach (var grp in dateGroups) {
                grp.Sort ();
            }
            dateGroups.Sort ((a, b) => b.Date.CompareTo (a.Date));
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            if (reloadScheduled) {
                reloadScheduled = false;
                IsLoading = false;
                Load (true);
            }

            if (subscriptionSyncFinished != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        public event EventHandler Updated;

        private void OnUpdated ()
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }

            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void BeginUpdate ()
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }
            updateMode = UpdateMode.Batch;
        }

        private void EndUpdate ()
        {
            updateMode = UpdateMode.Immediate;
            OnUpdated ();
        }

        public void Reload ()
        {
            if (IsLoading) {
                return;
            }

            startFrom = Time.UtcNow;
            dateGroups.Clear ();
            HasMore = true;

            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            if (syncManager.IsRunning) {
                // Fake loading until initial sync has finished
                IsLoading = true;
                OnUpdated ();

                reloadScheduled = true;
                if (subscriptionSyncFinished == null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
                }
            } else {
                Load (true);
            }
        }

        public void LoadMore ()
        {
            Load (false);
        }

        private async void Load (bool initialLoad)
        {
            if (IsLoading || !HasMore) {
                return;
            }

            IsLoading = true;
            var client = ServiceContainer.Resolve<ITogglClient> ();
            OnUpdated ();

            try {
                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                var endTime = startFrom;
                var startTime = startFrom = endTime - TimeSpan.FromDays (4);

                bool useLocal = false;

                if (initialLoad) {
                    useLocal = true;
                    startTime = startFrom = endTime - TimeSpan.FromDays (9);
                }

                // Try with latest data from server first:
                if (!useLocal) {
                    const int numDays = 5;
                    try {
                        var minStart = endTime;
                        var jsonEntries = await client.ListTimeEntries (endTime, numDays);

                        BeginUpdate ();
                        var entries = await dataStore.ExecuteInTransactionAsync (ctx =>
                                      jsonEntries.Select (json => json.Import (ctx)).ToList ());

                        // Add entries to list:
                        foreach (var entry in entries) {
                            AddOrUpdateEntry (entry);

                            if (entry.StartTime < minStart) {
                                minStart = entry.StartTime;
                            }
                        }

                        startTime = minStart;
                        HasMore = (endTime.Date - minStart.Date).Days > 0;
                    } catch (Exception exc) {
                        var log = ServiceContainer.Resolve<ILogger> ();
                        if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                            log.Info (Tag, exc, "Failed to fetch time entries {1} days up to {0}", endTime, numDays);
                        } else {
                            log.Warning (Tag, exc, "Failed to fetch time entries {1} days up to {0}", endTime, numDays);
                        }

                        useLocal = true;
                    }
                }

                // Fall back to local data:
                if (useLocal) {
                    var store = ServiceContainer.Resolve<IDataStore> ();
                    var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();

                    var baseQuery = store.Table<TimeEntryData> ()
                                    .OrderByDescending (r => r.StartTime)
                                    .Where (r => r.State != TimeEntryState.New
                                            && r.DeletedAt == null
                                            && r.UserId == userId);
                    var entries = await baseQuery
                                  .Where (r => r.StartTime <= endTime && r.StartTime > startTime)
                                  .ToListAsync();

                    BeginUpdate ();
                    foreach (var entry in entries) {
                        AddOrUpdateEntry (entry);
                    }

                    if (!initialLoad) {
                        var count = await baseQuery
                                    .Where (r => r.StartTime <= startTime)
                                    .CountAsync();
                        HasMore = count > 0;
                    }
                }
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (Tag, exc, "Failed to fetch time entries");
            } finally {
                IsLoading = false;
                EndUpdate ();
            }
        }

        public IEnumerable<DateGroup> DateGroups
        {
            get { return dateGroups; }
        }

        public IEnumerable<object> Data
        {
            get {
                foreach (var grp in dateGroups) {
                    yield return grp;
                    foreach (var data in grp.DataObjects) {
                        yield return data;
                    }
                }
            }
        }

        public long Count
        {
            get { return dateGroups.Count + dateGroups.Sum (g => g.DataObjects.Count); }
        }

        public bool HasMore { get; private set; }

        public bool IsLoading { get; private set; }

        public class DateGroup
        {
            private readonly DateTime date;
            private readonly List<TimeEntryData> dataObjects = new List<TimeEntryData> ();

            public DateGroup (DateTime date)
            {
                this.date = date.Date;
            }

            public DateTime Date
            {
                get { return date; }
            }

            public List<TimeEntryData> DataObjects
            {
                get { return dataObjects; }
            }

            public event EventHandler Updated;

            private void OnUpdated ()
            {
                var handler = Updated;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            }

            public void Add (TimeEntryData dataObject)
            {
                dataObjects.Add (dataObject);
                OnUpdated ();
            }

            public void Remove (TimeEntryData dataObject)
            {
                dataObjects.Remove (dataObject);
                OnUpdated ();
            }

            public void Sort ()
            {
                dataObjects.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                OnUpdated ();
            }
        }

        private enum UpdateMode {
            Immediate,
            Batch,
        }
    }
}