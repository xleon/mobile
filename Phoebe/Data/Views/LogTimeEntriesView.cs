using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IDataStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class LogTimeEntriesView : ICollectionDataView<object>, IDisposable
    {
        private static readonly string Tag = "LogTimeEntriesView";

        private readonly ObservableCollection<object> itemCollection = new ObservableCollection<object> ();
        private readonly List<DateGroup> dateGroups = new List<DateGroup> ();
        private UpdateMode updateMode = UpdateMode.Batch;
        private DateTime startFrom;
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;

        private bool reloadScheduled;
        private bool isLoading;
        private bool hasMore;
        private int lastItemNumber;

        // for Undo/Restore operations
        private TimeEntryData lastRemovedItem;

        public LogTimeEntriesView ()
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

        public async void ContinueTimeEntry (TimeEntryData timeEntryData)
        {
            await TimeEntryModel.ContinueTimeEntryDataAsync (timeEntryData);

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }

        public async void StopTimeEntry (TimeEntryData timeEntryData)
        {
            await TimeEntryModel.StopAsync (timeEntryData);

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
        }

        public void RestoreItemFromUndo ()
        {
            if (lastRemovedItem != null) {
                AddOrUpdateEntry (lastRemovedItem);
                lastRemovedItem = null;
            }
        }

        public async void RemoveItemWithUndo (TimeEntryData data)
        {
            // Remove previous if exists
            RemoveItemPermanently (lastRemovedItem);
            if (data.State == TimeEntryState.Running) {
                await TimeEntryModel.StopAsync (data);
            }
            lastRemovedItem = data;
            RemoveEntry (data);
        }

        public void ConfirmItemRemove ()
        {
            RemoveItemPermanently (lastRemovedItem);
        }

        private async void RemoveItemPermanently (TimeEntryData itemToRemove)
        {
            if (itemToRemove != null) {
                await TimeEntryModel.DeleteAsync (itemToRemove);
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
                if (lastRemovedItem != null && lastRemovedItem.Matches (entry)) {
                    return;
                }
                AddOrUpdateEntry (new TimeEntryData (entry));
            }
        }

        private void AddOrUpdateEntry (TimeEntryData entry)
        {
            int groupIndex;
            int newIndex;
            int oldIndex = -1;
            NotifyCollectionChangedAction entryAction;

            TimeEntryData existingEntry;
            DateGroup grp;
            bool isNewGroup = false;

            if (FindExistingEntry (entry, out grp, out existingEntry)) {
                if (entry.StartTime != existingEntry.StartTime) {
                    var date = entry.StartTime.ToLocalTime ().Date;
                    oldIndex = GetTimeEntryIndex (existingEntry);
                    if (grp.Date != date) {
                        // Need to move entry: //TODO: remove dateGroup too?
                        grp.Remove (existingEntry);
                        DispatchCollectionEvent (grp, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, GetDateGroupIndex (grp), -1));

                        grp = GetGroupFor (entry, out isNewGroup);
                        grp.Add (entry);
                        entryAction = NotifyCollectionChangedAction.Move;
                        Sort ();
                    } else {
                        grp.DataObjects.UpdateData (entry);
                        Sort ();
                        newIndex = GetTimeEntryIndex (entry);
                        if (newIndex != oldIndex) {
                            DispatchCollectionEvent (entry, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newIndex, oldIndex));
                        }
                        entryAction = NotifyCollectionChangedAction.Replace;
                    }
                } else {
                    grp.DataObjects.UpdateData (entry);
                    entryAction = NotifyCollectionChangedAction.Replace;
                }
            } else {
                grp = GetGroupFor (entry, out isNewGroup);
                grp.Add (entry);
                Sort ();
                entryAction = NotifyCollectionChangedAction.Add;
            }

            // Updated entry.
            newIndex = GetTimeEntryIndex (entry);
            DispatchCollectionEvent (entry, CollectionEventBuilder.GetEvent (entryAction, newIndex, oldIndex));

            // Update group.
            groupIndex = GetDateGroupIndex (grp);
            var groupAction = isNewGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
            DispatchCollectionEvent (grp, CollectionEventBuilder.GetEvent (groupAction, groupIndex, oldIndex));
        }

        private void RemoveEntry (TimeEntryData entry)
        {
            int groupIndex;
            int entryIndex;
            NotifyCollectionChangedAction groupAction = NotifyCollectionChangedAction.Replace;
            DateGroup grp;
            TimeEntryData oldEntry;

            if (FindExistingEntry (entry, out grp, out oldEntry)) {
                entryIndex = GetTimeEntryIndex (oldEntry);
                groupIndex = GetDateGroupIndex (grp);
                grp.Remove (oldEntry);
                if (grp.DataObjects.Count == 0) {
                    dateGroups.Remove (grp);
                    groupAction = NotifyCollectionChangedAction.Remove;
                }

                // The order affects how the collection is updated.
                DispatchCollectionEvent (entry, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, entryIndex, -1));
                DispatchCollectionEvent (grp, CollectionEventBuilder.GetEvent (groupAction, groupIndex, -1));
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

        private int GetTimeEntryIndex (TimeEntryData dataObject)
        {
            int count = 0;
            foreach (var grp in dateGroups) {
                count++;
                // Iterate by entry list.
                foreach (var obj in grp.DataObjects) {
                    if (dataObject.Matches (obj)) {
                        return count;
                    }
                    count++;
                }
            }
            return -1;
        }

        private int GetDateGroupIndex (DateGroup dateGroup)
        {
            var count = 0;
            foreach (var grp in dateGroups) {
                if (grp.Date == dateGroup.Date) {
                    return count;
                }
                count += grp.DataObjects.Count + 1;
            }
            return -1;
        }

        private DateGroup GetGroupFor (TimeEntryData dataObject, out bool isNewGroup)
        {
            isNewGroup = false;
            var date = dataObject.StartTime.ToLocalTime ().Date;
            var grp = dateGroups.FirstOrDefault (g => g.Date == date);
            if (grp == null) {
                grp = new DateGroup (date);
                dateGroups.Add (grp);
                isNewGroup = true;
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

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private async void DispatchCollectionEvent (object item, NotifyCollectionChangedEventArgs args)
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }

            // Update list
            await UpdateCollection (item, args);

            var handler = CollectionChanged;
            if (handler != null) {
                handler (this, args);
            }
        }

        private void BeginUpdate ()
        {
            if (updateMode != UpdateMode.Immediate) {
                return;
            }
            lastItemNumber = Count;
            updateMode = UpdateMode.Batch;
        }

        private void EndUpdate ()
        {
            updateMode = UpdateMode.Immediate;
            if (Count > lastItemNumber) {
                DispatchCollectionEvent (new object(), CollectionEventBuilder.GetRangeEvent (NotifyCollectionChangedAction.Add, lastItemNumber, Count - lastItemNumber));
            }
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
                                    .OrderBy (r => r.StartTime, false)
                                    .Where (r => r.State != TimeEntryState.New
                                            && r.DeletedAt == null
                                            && r.UserId == userId);
                    var entries = await baseQuery
                                  .QueryAsync (r => r.StartTime <= endTime
                                               && r.StartTime > startTime);

                    BeginUpdate ();
                    foreach (var entry in entries) {
                        AddOrUpdateEntry (entry);
                    }

                    if (!initialLoad) {
                        var count = await baseQuery
                                    .CountAsync (r => r.StartTime <= startTime);
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

        public IEnumerable<object> Data
        {
            get {
                return itemCollection;
            }
        }

        public IEnumerable<DateGroup> DateGroups
        {
            get { return dateGroups; }
        }

        private IEnumerable<object> UpdatedList
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

        public int Count
        {
            get {
                var itemsCount = dateGroups.Sum (g => g.DataObjects.Count);
                return dateGroups.Count + itemsCount;
            }
        }

        public event EventHandler OnHasMoreChanged;

        public bool HasMore
        {
            get {
                return hasMore;
            }
            private set {

                if (hasMore == value) {
                    return;
                }

                hasMore = value;

                if (OnHasMoreChanged != null) {
                    OnHasMoreChanged (this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler OnIsLoadingChanged;

        public bool IsLoading
        {
            get {
                return isLoading;
            }
            private set {

                if (isLoading  == value) {
                    return;
                }

                isLoading = value;

                if (OnIsLoadingChanged != null) {
                    OnIsLoadingChanged (this, EventArgs.Empty);
                }
            }
        }

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

        public class TimeEntryHolder
        {
            private TimeEntryData timeEntry;
            private ProjectData project;
            private ClientData client;
            private TaskData task;
            private int numberOfTags;

            public TimeEntryHolder (TimeEntryData timeEntry)
            {
                this.timeEntry = timeEntry;
                project = new ProjectData ();
                client = new ClientData ();
                task = new TaskData ();
            }

            public TimeEntryData TimeEntryData
            {
                get { return timeEntry; }
            }

            public ProjectData ProjectData
            {
                get { return project; }
            }

            public ClientData ClientData
            {
                get { return client; }
            }

            public TaskData TaskData
            {
                get { return task; }
            }

            public Guid Id
            {
                get { return TimeEntryData.Id; }
            }

            public int NumberOfTags
            {
                get { return numberOfTags; }
            }

            public string ProjectName
            {
                get {
                    return project.Name;
                }
            }

            public string ClientName
            {
                get {
                    return client.Name;
                }
            }

            public string TaskName
            {
                get {
                    return task.Name;
                }
            }

            public string Description
            {
                get {
                    return timeEntry.Description;
                }
            }

            public int Color
            {
                get {
                    return (project.Id != Guid.Empty) ? project.Color : -1;
                }
            }

            public TimeEntryState State
            {
                get {
                    return timeEntry.State;
                }
            }

            public bool IsBillable
            {
                get {
                    return timeEntry.IsBillable;
                }
            }

            public async Task LoadAsync ()
            {
                numberOfTags = 0;

                if (timeEntry.ProjectId.HasValue) {
                    project = await GetProjectDataAsync (timeEntry.ProjectId.Value);
                    if (project.ClientId.HasValue) {
                        client = await GetClientDataAsync (project.ClientId.Value);
                    }
                }

                if (timeEntry.TaskId.HasValue) {
                    task = await GetTaskDataAsync (timeEntry.TaskId.Value);
                }

                numberOfTags = await GetNumberOfTagsAsync (timeEntry.Id);
            }

            public async Task UpdateAsync (TimeEntryData data)
            {
                timeEntry = new TimeEntryData (data);
                project = await UpdateProject (data, ProjectData);
                client = await UpdateClient (project, ClientData);
                task = await UpdateTask (data, TaskData);
                numberOfTags = await GetNumberOfTagsAsync (data.Id);
            }

            private async Task<ProjectData> GetProjectDataAsync (Guid projectGuid)
            {
                var store = ServiceContainer.Resolve<IDataStore> ();
                var projectList = await store.Table<ProjectData> ()
                                  .Take (1).QueryAsync (m => m.Id == projectGuid);
                return projectList.First ();
            }

            private async Task<TaskData> GetTaskDataAsync (Guid taskId)
            {
                var store = ServiceContainer.Resolve<IDataStore> ();
                var taskList = await store.Table<TaskData> ()
                               .Take (1).QueryAsync (m => m.Id == taskId);
                return taskList.First ();
            }

            private async Task<ClientData> GetClientDataAsync (Guid clientId)
            {
                var store = ServiceContainer.Resolve<IDataStore> ();
                var clientList = await store.Table<ClientData> ()
                                 .Take (1).QueryAsync (m => m.Id == clientId);
                return clientList.First ();
            }

            private Task<int> GetNumberOfTagsAsync (Guid timeEntryGuid)
            {
                var store = ServiceContainer.Resolve<IDataStore> ();
                return store.Table<TimeEntryTagData>()
                       .Where (t => t.TimeEntryId == timeEntryGuid)
                       .CountAsync ();
            }

            private async Task<ProjectData> UpdateProject (TimeEntryData newTimeEntry, ProjectData oldProjectData)
            {
                if (!newTimeEntry.ProjectId.HasValue) {
                    return new ProjectData ();
                }

                if (oldProjectData.Id == Guid.Empty && newTimeEntry.ProjectId.HasValue) {
                    return await GetProjectDataAsync (newTimeEntry.ProjectId.Value);
                }

                if (newTimeEntry.ProjectId.Value != oldProjectData.Id) {
                    return await GetProjectDataAsync (newTimeEntry.ProjectId.Value);
                }

                return oldProjectData;
            }

            private async Task<ClientData> UpdateClient (ProjectData projectData, ClientData oldClientData)
            {
                if (!projectData.ClientId.HasValue) {
                    return new ClientData ();
                }

                if (oldClientData == null && projectData.ClientId.HasValue) {
                    return await GetClientDataAsync (projectData.ClientId.Value);
                }

                if (projectData.ClientId.Value != oldClientData.Id) {
                    return await GetClientDataAsync (projectData.ClientId.Value);
                }

                return oldClientData;
            }

            private async Task<TaskData> UpdateTask (TimeEntryData newTimeEntry, TaskData oldTaskData)
            {
                if (!newTimeEntry.TaskId.HasValue) {
                    return new TaskData ();
                }

                if (oldTaskData == null && newTimeEntry.TaskId.HasValue) {
                    return await GetTaskDataAsync (newTimeEntry.TaskId.Value);
                }

                if (newTimeEntry.TaskId.Value != oldTaskData.Id) {
                    return await GetTaskDataAsync (newTimeEntry.TaskId.Value);
                }

                return oldTaskData;
            }
        }

        private enum UpdateMode {
            Immediate,
            Batch,
        }

        private async Task UpdateCollection (object data, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                var createHolderTaskList = new List<Task> ();
                var currentItems = new List<object> (UpdatedList);
                itemCollection.Clear ();

                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++) {
                    var item = currentItems [i];
                    if (item is TimeEntryData) {
                        var entryData = (TimeEntryData)item;
                        var timeEntryHolder = new TimeEntryHolder (entryData);
                        itemCollection.Add (item);
                        createHolderTaskList.Add (timeEntryHolder.LoadAsync ());
                    } else {
                        itemCollection.Add (item);
                    }
                }

                await Task.WhenAll (createHolderTaskList);
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add) {
                if (e.NewItems.Count == 1) {
                    if (data is TimeEntryData) {
                        var newHolder = new TimeEntryHolder ((TimeEntryData)data);
                        await newHolder.LoadAsync ();
                        itemCollection.Insert (e.NewStartingIndex, newHolder);
                    } else {
                        itemCollection.Insert (e.NewStartingIndex, data);
                    }
                } else {

                    var createHolderTaskList = new List<Task> ();
                    var currentItems = new List<object> (UpdatedList);
                    if (e.NewStartingIndex == 0) {
                        itemCollection.Clear ();
                    }

                    for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++) {
                        var item = currentItems [i];

                        if (item is TimeEntryData) {
                            var entryData = (TimeEntryData)item;
                            var timeEntryHolder = new TimeEntryHolder (entryData);

                            if (i == itemCollection.Count) {
                                itemCollection.Insert (i, timeEntryHolder);
                                createHolderTaskList.Add (timeEntryHolder.LoadAsync ());
                            } else if (i > itemCollection.Count) {
                                itemCollection.Add (timeEntryHolder);
                                createHolderTaskList.Add (timeEntryHolder.LoadAsync ());
                            }  else {
                                itemCollection [i] = timeEntryHolder;
                                createHolderTaskList.Add (timeEntryHolder.UpdateAsync (entryData));
                            }
                        } else {
                            if (i == itemCollection.Count) {
                                itemCollection.Insert (i, item);
                            } else if (i > itemCollection.Count) {
                                itemCollection.Add (item);
                            }
                        }
                    }
                    await Task.WhenAll (createHolderTaskList);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                itemCollection.Move (e.OldStartingIndex, e.NewStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                itemCollection.RemoveAt (e.OldStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                if (data is TimeEntryData) {
                    var oldHolder = (TimeEntryHolder)itemCollection.ElementAt (e.NewStartingIndex);
                    await oldHolder.UpdateAsync ((TimeEntryData)data);
                    itemCollection [e.NewStartingIndex] = oldHolder;
                } else {
                    itemCollection [e.NewStartingIndex] = data;
                }
            }
        }
    }
}
