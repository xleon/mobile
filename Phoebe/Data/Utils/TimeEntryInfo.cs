using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;
using System.Threading.Tasks;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    public class TimeEntryInfo
    {
        public ProjectData ProjectData { get; private set; }
        public ClientData ClientData { get; private set; }
        public TaskData TaskData { get; private set; }
        public string Description { get; private set; }
        public int Color { get; private set; }
        public bool IsBillable { get; private set; }
        public int NumberOfTags { get; private set; }

        TimeEntryInfo ()
        {
        }

        public static async Task<TimeEntryInfo> LoadAsync (TimeEntryData timeEntryData)
        {
            var info = new TimeEntryInfo ();
            if (timeEntryData.ProjectId.HasValue) {
                info.ProjectData = await GetProjectDataAsync (timeEntryData.ProjectId.Value);
                if (info.ProjectData.ClientId.HasValue) {
                    info.ClientData = await GetClientDataAsync (info.ProjectData.ClientId.Value);
                }
            }

            if (timeEntryData.TaskId.HasValue) {
                info.TaskData = await GetTaskDataAsync (timeEntryData.TaskId.Value);
            }

            info.NumberOfTags = await GetNumberOfTagsAsync (timeEntryData.Id);

            return info;
        }

        private static async Task<ProjectData> GetProjectDataAsync (Guid projectGuid)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var projectList = await store.Table<ProjectData> ()
                              .Where (m => m.Id == projectGuid)
                              .Take (1)
                              .ToListAsync ();
            return projectList.First ();
        }

        private static async Task<TaskData> GetTaskDataAsync (Guid taskId)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var taskList = await store.Table<TaskData> ()
                           .Where (m => m.Id == taskId)
                           .Take (1)
                           .ToListAsync ();
            return taskList.First ();
        }

        private static async Task<ClientData> GetClientDataAsync (Guid clientId)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var clientList = await store.Table<ClientData> ()
                             .Where (m => m.Id == clientId)
                             .Take (1)
                             .ToListAsync ();
            return clientList.First ();
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
    }
}

