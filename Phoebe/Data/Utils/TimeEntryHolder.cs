using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    public class TimeEntryHolder
    {
        private List<TimeEntryData> timeEntryDataList = new List<TimeEntryData> ();
        private TimeEntryData timeEntryData;
        private ProjectData projectData;
        private ClientData clientData;
        private TaskData taskData;
        private int numberOfTags;

        public TimeEntryHolder (IEnumerable<TimeEntryData> timeEntryGroup)
        {
            if (timeEntryGroup == null || !timeEntryGroup.Any ()) {
                throw new ArgumentException ("Must be specified", "timeEntryGroup");
            }

            timeEntryDataList.AddRange (timeEntryGroup);
            timeEntryData = new TimeEntryData (timeEntryGroup.Last ());
            projectData = new ProjectData ();
            clientData = new ClientData ();
            taskData = new TaskData ();
        }

        public TimeSpan TotalDuration
        {
            get {
                TimeSpan totalDuration = TimeSpan.Zero;
                foreach (var item in timeEntryDataList) {
                    totalDuration += TimeEntryModel.GetDuration (item, Time.UtcNow);
                }
                return totalDuration;
            }
        }

        public IList<string> TimeEntryGuids
        {
            get {
                return timeEntryDataList.AsEnumerable ().Select (r => r.Id.ToString ()).ToList ();
            }
        }

        public IList<TimeEntryData> TimeEntryDataList
        {
            get {
                return timeEntryDataList;
            }
        }

        public TimeEntryData TimeEntryData
        {
            get { return timeEntryData; }
        }

        public ProjectData ProjectData
        {
            get { return projectData; }
        }

        public ClientData ClientData
        {
            get { return clientData; }
        }

        public TaskData TaskData
        {
            get { return taskData; }
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
                return projectData.Name;
            }
        }

        public string ClientName
        {
            get {
                return clientData.Name;
            }
        }

        public string TaskName
        {
            get {
                return taskData.Name;
            }
        }

        public string Description
        {
            get {
                return timeEntryData.Description;
            }
        }

        public int Color
        {
            get {
                return (projectData.Id != Guid.Empty) ? projectData.Color : -1;
            }
        }

        public TimeEntryState State
        {
            get {
                return timeEntryData.State;
            }
        }

        public bool IsBillable
        {
            get {
                return timeEntryData.IsBillable;
            }
        }

        public async Task LoadAsync ()
        {
            numberOfTags = 0;

            if (timeEntryData.ProjectId.HasValue) {
                projectData = await GetProjectDataAsync (timeEntryData.ProjectId.Value).ConfigureAwait (false);
                if (projectData.ClientId.HasValue) {
                    clientData = await GetClientDataAsync (projectData.ClientId.Value).ConfigureAwait (false);
                }
            }

            if (timeEntryData.TaskId.HasValue) {
                taskData = await GetTaskDataAsync (timeEntryData.TaskId.Value).ConfigureAwait (false);
            }
            numberOfTags = await GetNumberOfTagsAsync (timeEntryData.Id).ConfigureAwait (false);
        }

        public async Task UpdateAsync (IEnumerable<TimeEntryData> timeEntryGroup)
        {
            timeEntryData = new TimeEntryData (timeEntryGroup.Last ());
            timeEntryDataList.Clear ();
            timeEntryDataList.AddRange (timeEntryGroup);

            projectData = await UpdateProject (timeEntryData, ProjectData).ConfigureAwait (false);
            clientData = await UpdateClient (projectData, ClientData).ConfigureAwait (false);
            taskData = await UpdateTask (timeEntryData, TaskData).ConfigureAwait (false);
            numberOfTags = await GetNumberOfTagsAsync (timeEntryData.Id).ConfigureAwait (false);
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
                return await GetProjectDataAsync (newTimeEntry.ProjectId.Value).ConfigureAwait (false);
            }

            if (newTimeEntry.ProjectId.Value != oldProjectData.Id) {
                return await GetProjectDataAsync (newTimeEntry.ProjectId.Value).ConfigureAwait (false);
            }

            return oldProjectData;
        }

        private async Task<ClientData> UpdateClient (ProjectData newProjectData, ClientData oldClientData)
        {
            if (!newProjectData.ClientId.HasValue) {
                return new ClientData ();
            }

            if (oldClientData == null && newProjectData.ClientId.HasValue) {
                return await GetClientDataAsync (newProjectData.ClientId.Value).ConfigureAwait (false);
            }

            if (newProjectData.ClientId.Value != oldClientData.Id) {
                return await GetClientDataAsync (newProjectData.ClientId.Value).ConfigureAwait (false);
            }

            return oldClientData;
        }

        private async Task<TaskData> UpdateTask (TimeEntryData newTimeEntry, TaskData oldTaskData)
        {
            if (!newTimeEntry.TaskId.HasValue) {
                return new TaskData ();
            }

            if (oldTaskData == null && newTimeEntry.TaskId.HasValue) {
                return await GetTaskDataAsync (newTimeEntry.TaskId.Value).ConfigureAwait (false);
            }

            if (newTimeEntry.TaskId.Value != oldTaskData.Id) {
                return await GetTaskDataAsync (newTimeEntry.TaskId.Value).ConfigureAwait (false);
            }

            return oldTaskData;
        }
    }
}

