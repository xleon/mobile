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

            case DataTag.TimeEntryRemoveWithUndo:
                return TimeEntryRemoveWithUndo;

            case DataTag.TimeEntryRestoreFromUndo:
                return TimeEntryRestoreFromUndo;

            case DataTag.TimeEntryRemove:
                return TimeEntryRemove;

            default:
                throw new ActionNotFoundException (tag, typeof (DispatcherRegister));
            }
        }

        static Task<IDataMsg> TimeEntryLoadFromServer (IDataMsg msg)
        {
            return msg.MatchDataAsync<TimeEntryJsonMsg, IDataMsg> (
            async jsonMsg => {
                var storeMsgs =
                    await dataStore.ExecuteInTransactionWithMessagesAsync (ctx =>
                            jsonMsg.Messages.ForEach (json => json.Import (ctx)));

                var entryMsgs =
                    storeMsgs.Select (x => Tuple.Create ((TimeEntryData)x.Data, x.Action)).ToList ();

                return DataMsg.Success (new TimeEntryMsg (entryMsgs), msg.Tag, msg.Dir);
            }, ex => msg);
        }

        // Set initial pagination Date to the beginning of the next day.
        // So, we can include all entries created -Today-.
        static DateTime paginationDate = Time.UtcNow.Date.AddDays (1);
        static async Task<IDataMsg> TimeEntryLoad (IDataMsg msg)
        {
            var endDate = paginationDate;
            var startDate = await GetDatesByDays (endDate, Literals.TimeEntryLoadDays);

            // Always fall back to local data:
            var store = ServiceContainer.Resolve<IDataStore> ();
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
            var baseQuery =
                store.Table<TimeEntryData> ()
                .Where (r =>
                        r.State != TimeEntryState.New &&
                        r.StartTime >= startDate && r.StartTime < endDate &&
                        r.DeletedAt == null &&
                        r.UserId == userId)
                .Take (Literals.TimeEntryLoadMaxInit);

            var entryMsgs =
                (await baseQuery.OrderByDescending (r => r.StartTime)
                 .ToListAsync ())
                .Select (x => Tuple.Create (x, DataAction.Put))
                .ToArray ();
            paginationDate = entryMsgs.Length > 0 ? startDate : endDate;

            // Try to update with latest data from server with old paginationDate to get the same data
            Dispatcher.Send (DataTag.TimeEntryLoadFromServer, endDate);

            return DataMsg.Success (new TimeEntryMsg (entryMsgs), msg.Tag, DataDir.None);
        }

        static async Task<IDataMsg> TimeEntryStop (IDataMsg msg)
        {
            var timeEntryData = msg.ForceGetData<TimeEntryData> ();

            // Code from TimeEntryModel.StopAsync
            if (timeEntryData.State != TimeEntryState.Running) {
                throw new InvalidOperationException (
                    String.Format ("Cannot stop a time entry in {0} state.", timeEntryData.State));
            }

            // Mutate data
            timeEntryData = MutateData (timeEntryData, data => {
                data.State = TimeEntryState.Finished;
                data.StopTime = Time.UtcNow;
            });

            // Save TimeEntryData
            var newData = await dataStore.PutAsync (timeEntryData);
            return DataMsg.Success (new TimeEntryMsg (newData, DataAction.Put), msg.Tag, DataDir.Outcoming);
        }

        static Task<IDataMsg> TimeEntryRemoveWithUndo (IDataMsg msg)
        {
            // Speculative delete: don't touch the db for now
            var entryMsg = new TimeEntryMsg (msg.ForceGetData<TimeEntryData> (), DataAction.Delete);
            return Task.Run<IDataMsg> (() => DataMsg.Success (entryMsg, msg.Tag, DataDir.None));
        }

        static Task<IDataMsg> TimeEntryRestoreFromUndo (IDataMsg msg)
        {
            // The entry wasn't really deleted, see RemoveTimeEntryWithUndo
            var entryMsg = new TimeEntryMsg (msg.ForceGetData<TimeEntryData> (), DataAction.Put);
            return Task.Run<IDataMsg> (() => DataMsg.Success (entryMsg, msg.Tag, DataDir.None));
        }

        static async Task<IDataMsg> TimeEntryRemove (IDataMsg msg)
        {
            var entries = msg.ForceGetData<IList<TimeEntryData>> ();

            // Code from TimeEntryModel.DeleteTimeEntryDataAsync
            var tasks = entries.Select (async data => {
                if (data.RemoteId == null) {
                    // We can safely delete the item as it has not been synchronized with the server yet
                    await dataStore.DeleteAsync (data);
                } else {
                    // Need to just mark this item as deleted so that it could be synced with the server
                    var newData = new TimeEntryData (data);
                    newData.DeletedAt = Time.UtcNow;

                    MarkDirty (newData);

                    await dataStore.PutAsync (newData);
                }
            });
            await Task.WhenAll (tasks);

            var entryMsgs = entries.Select (x => Tuple.Create (x, DataAction.Delete)).ToArray();
            return DataMsg.Success (new TimeEntryMsg (entryMsgs), msg.Tag, DataDir.Outcoming);
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

