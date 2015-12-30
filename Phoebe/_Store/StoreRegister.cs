using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Models;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe
{
    public static partial class Store
    {
        static Func<DataMsgUntyped, Task<StoreResultUntyped>> GetCallback (string tag)
        {
            switch (tag) {
            default:
                return null;
            }
        }

        #region TimeEntryInfo
        static async Task<TimeEntryInfo> LoadAsync (TimeEntry timeEntryData)
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

        private static async Task<ProjectData> UpdateProject (TimeEntry newTimeEntry, ProjectData oldProjectData)
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

        private static async Task<TaskData> UpdateTask (TimeEntry newTimeEntry, TaskData oldTaskData)
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

