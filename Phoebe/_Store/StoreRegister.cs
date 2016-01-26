using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static partial class Store
    {
        static IDataStore dataStore = ServiceContainer.Resolve<IDataStore> ();

        static Func<IDataMsg, Task<IDataMsg>> GetAction (DataTag tag)
        {
            switch (tag) {
            case DataTag.TimeEntryLoad:
                return TimeEntryLoad;

            case DataTag.TimeEntryLoadFromServer:
                return TimeEntryLoadFromServer;

            case DataTag.TimeEntryStop:
                return TimeEntryStop;

            case DataTag.TimeEntryRemove:
                return TimeEntryRemove;

            // These operations don't really do anything with the database
            case DataTag.TimeEntryRemoveWithUndo:
            case DataTag.TimeEntryRestoreFromUndo:
                return DispatcherRegister.LetGoThrough;
                
            default:
                throw new ActionNotFoundException (tag, typeof (DispatcherRegister));
            }
        }

        static async Task<IDataMsg> TimeEntryLoadFromServer (IDataMsg msg)
        {
            var jsonEntries = msg.ForceGetData<List<TimeEntryJson>> ();

            var dbMsgs = await dataStore.ExecuteInTransactionWithMessagesAsync (ctx =>
                    jsonEntries.ForEach (json => json.Import (ctx)));

            var entryMsg = new TimeEntryMsg (DataDir.Incoming, dbMsgs.Select (
                x => Tuple.Create (x.Action.ToVerb (), (TimeEntryData)x.Data)));

            return DataMsg.Success (msg.Tag, entryMsg);
        }

        // Set initial pagination Date to the beginning of the next day.
        // So, we can include all entries created -Today-.
        static DateTime paginationDate = Time.UtcNow.Date.AddDays (1);
        static async Task<IDataMsg> TimeEntryLoad (IDataMsg msg)
        {
            var startDate = await GetDatesByDays (paginationDate, Literals.TimeEntryLoadDays);

            // Always fall back to local data:
            var store = ServiceContainer.Resolve<IDataStore> ();
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
            var baseQuery =
                store.Table<TimeEntryData> ()
                .Where (r =>
                        r.State != TimeEntryState.New &&
                        r.StartTime >= startDate && r.StartTime < paginationDate &&
                        r.DeletedAt == null &&
                        r.UserId == userId)
                .Take (Literals.TimeEntryLoadMaxInit);

            var dbMsgs = (await baseQuery.OrderByDescending (r => r.StartTime).ToListAsync ())
                .Select (x => Tuple.Create (DataVerb.Post, x)).ToList ();

            // Try to update with latest data from server with old paginationDate to get the same data
            Dispatcher.Send (DataTag.TimeEntryLoadFromServer, paginationDate);
            paginationDate = dbMsgs.Count > 0 ? startDate : paginationDate;

            // TODO: Check if there're entries in the db that hasn't been synced
            return DataMsg.Success (msg.Tag, new TimeEntryMsg (DataDir.Incoming, dbMsgs));
        }

        static async Task<IDataMsg> TimeEntryStop (IDataMsg msg)
        {
            var entryMsg = msg.ForceGetData<TimeEntryMsg> ();

            var newEntries = new List<Tuple<DataVerb, TimeEntryData>> ();
            foreach (var tuple in entryMsg) {
                var entryData = tuple.Item2;

                // Code from TimeEntryModel.StopAsync
                if (entryData.State != TimeEntryState.Running) {
                    throw new InvalidOperationException (
                        String.Format ("Cannot stop a time entry in {0} state.", entryData.State));
                }

                // Mutate data
                entryData = MutateData (entryData, data => {
                    data.State = TimeEntryState.Finished;
                    data.StopTime = Time.UtcNow;
                });

                // Save TimeEntryData
                var newData = await dataStore.PutAsync (entryData);

                newEntries.Add (Tuple.Create (tuple.Item1, newData));
            }

            return DataMsg.Success (msg.Tag, new TimeEntryMsg (entryMsg.Dir, newEntries));
        }

        static async Task<IDataMsg> TimeEntryRemove (IDataMsg msg)
        {
            var entryMsg = msg.ForceGetData<TimeEntryMsg> ();

            var newEntries = new List<Tuple<DataVerb, TimeEntryData>> ();
            foreach (var tuple in entryMsg) {
                var entryData = tuple.Item2;

                // Code from TimeEntryModel.DeleteTimeEntryDataAsync
                if (entryData.RemoteId == null) {
                    // We can safely delete the item as it has not been synchronized with the server yet
                    await dataStore.DeleteAsync (entryData);

                    // TODO: Ignore these entries? (They don't need to be synced and they're removed from the view)
                    // Or change the DataDir to Incoming?
                    // newEntries.Add (Tuple.Create (tuple.Item1, entryData));
                } else {
                    // Need to just mark this item as deleted so that it could be synced with the server
                    var newData = new TimeEntryData (entryData);
                    newData.DeletedAt = Time.UtcNow;

                    // TODO: Don't mark dirty, SyncOutManager takes care of syncing
                    MarkDirty (newData);

                    await dataStore.PutAsync (newData);
                    newEntries.Add (Tuple.Create (tuple.Item1, newData));
                }
            }

            return DataMsg.Success (msg.Tag, new TimeEntryMsg (entryMsg.Dir, newEntries));
        }

        #region Util
        // TODO: replace this method from the SQLite equivalent.
        static async Task<DateTime> GetDatesByDays (DateTime startDate, int numDays)
        {
            var baseQuery = dataStore.Table<TimeEntryData> ().Where (r =>
                            r.State != TimeEntryState.New &&
                            r.StartTime < startDate &&
                            r.DeletedAt == null);

            var entries = await baseQuery.ToListAsync ();
            if (entries.Count > 0) {
                var group = entries.OrderByDescending (r => r.StartTime).GroupBy (t => t.StartTime.Date).Take (numDays).LastOrDefault ();
                return group.Key;
            }
            return DateTime.MinValue;
        }

        static void MarkDirty (CommonData data)
        {
            data.IsDirty = true;
            data.ModifiedAt = Time.UtcNow;
            data.RemoteRejected = false;
        }

        static TimeEntryData MutateData (TimeEntryData timeEntryData, Action<TimeEntryData> mutator)
        {
            var newData = new TimeEntryData (timeEntryData);
            mutator (newData);
            MarkDirty (newData);
            return newData;
        }
        #endregion

        #region TimeEntryInfo
        public static async Task<TimeEntryInfo> LoadTimeEntryInfoAsync (TimeEntryData timeEntryData)
        {
            var info = new TimeEntryInfo ();
            info.ProjectData = timeEntryData.ProjectId.HasValue
                               ? await GetProjectDataAsync (timeEntryData.ProjectId.Value)
                               : new ProjectData ();
            info.ClientData = info.ProjectData.ClientId.HasValue
                              ? await GetClientDataAsync (info.ProjectData.ClientId.Value)
                              : new ClientData ();
            info.TaskData = timeEntryData.TaskId.HasValue
                            ? await GetTaskDataAsync (timeEntryData.TaskId.Value)
                            : new TaskData ();
            info.Description = timeEntryData.Description;
            info.Color = (info.ProjectData.Id != Guid.Empty) ? info.ProjectData.Color : -1;
            info.IsBillable = timeEntryData.IsBillable;
            info.NumberOfTags = await GetNumberOfTagsAsync (timeEntryData.Id);
            return info;
        }

        private static async Task<ProjectData> GetProjectDataAsync (Guid projectGuid)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            return await store.Table<ProjectData> ()
                   .Where (m => m.Id == projectGuid)
                   .FirstAsync ();
        }

        private static async Task<TaskData> GetTaskDataAsync (Guid taskId)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            return await store.Table<TaskData> ()
                   .Where (m => m.Id == taskId)
                   .FirstAsync ();
        }

        private static async Task<ClientData> GetClientDataAsync (Guid clientId)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            return await store.Table<ClientData> ()
                   .Where (m => m.Id == clientId)
                   .FirstAsync ();
        }

        private static Task<int> GetNumberOfTagsAsync (Guid timeEntryGuid)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            return store.Table<TimeEntryTagData>()
                   .Where (t => t.TimeEntryId == timeEntryGuid)
                   .CountAsync ();
        }

        private static async Task<ProjectData> UpdateProject (TimeEntryData newTimeEntry, ProjectData oldProjectData)
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

        private static async Task<ClientData> UpdateClient (ProjectData newProjectData, ClientData oldClientData)
        {
            if (!newProjectData.ClientId.HasValue) {
                return new ClientData ();
            }

            if (oldClientData == null && newProjectData.ClientId.HasValue) {
                return await GetClientDataAsync (newProjectData.ClientId.Value);
            }

            if (newProjectData.ClientId.Value != oldClientData.Id) {
                return await GetClientDataAsync (newProjectData.ClientId.Value);
            }

            return oldClientData;
        }

        private static async Task<TaskData> UpdateTask (TimeEntryData newTimeEntry, TaskData oldTaskData)
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
        #endregion
    }
}

