using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    public interface IHolder : IEquatable<IHolder>
    {
    }

    public interface ITimeHolder : IHolder
    {
        bool IsRunning { get; }
        TimeSpan TotalDuration { get; }
        DateTime StartTime { get; }
        TimeEntryData TimeEntryData { get; }
        bool Matches (TimeEntryData data);
        Task DeleteAsync();
    }

    public class TimeEntryHolder : IDisposable, ITimeHolder
    {
        private List<TimeEntryData> timeEntryDataList = new List<TimeEntryData> ();
        private TimeEntryData timeEntryData;
        private ProjectData projectData;
        private ClientData clientData;
        private TaskData taskData;
        private int numberOfTags;

        public DateTime StartTime
        {
            get { return timeEntryData.StartTime; }
        }

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

        public bool Equals (IHolder obj)
        {
            var other = obj as TimeEntryHolder;
            return other != null && other.Id == Id;
        }

        public bool Matches (TimeEntryData data)
        {
            return data.Id == Id;
        }

        public void Dispose ()
        {
            timeEntryDataList.Clear ();
            timeEntryData = new TimeEntryData ();
            projectData = new ProjectData ();
            clientData = new ClientData ();
            taskData = new TaskData ();
        }

        public bool IsRunning
        {
            get { return timeEntryData.State == TimeEntryState.Running; }
        }

        public TimeSpan TotalDuration
        {
            get { return TimeEntryModel.GetDuration (timeEntryData, Time.UtcNow); }
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
                projectData = await GetProjectDataAsync (timeEntryData.ProjectId.Value);
                if (projectData.ClientId.HasValue) {
                    clientData = await GetClientDataAsync (projectData.ClientId.Value);
                }
            }

            if (timeEntryData.TaskId.HasValue) {
                taskData = await GetTaskDataAsync (timeEntryData.TaskId.Value);
            }
            numberOfTags = await GetNumberOfTagsAsync (timeEntryData.Id);
        }

        public async Task UpdateAsync (IEnumerable<TimeEntryData> timeEntryGroup)
        {
            timeEntryData = new TimeEntryData (timeEntryGroup.Last ());
            timeEntryDataList.Clear ();
            timeEntryDataList.AddRange (timeEntryGroup);

            projectData = await UpdateProject (timeEntryData, ProjectData);
            clientData = await UpdateClient (projectData, ClientData);
            taskData = await UpdateTask (timeEntryData, TaskData);
            numberOfTags = await GetNumberOfTagsAsync (timeEntryData.Id);
        }

        public async Task DeleteAsync()
        {
            await TimeEntryModel.DeleteTimeEntryDataAsync (timeEntryData);
        }

        private async Task<ProjectData> GetProjectDataAsync (Guid projectGuid)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var projectList = await store.Table<ProjectData> ()
                              .Where (m => m.Id == projectGuid)
                              .Take (1)
                              .ToListAsync ();
            return projectList.First ();
        }

        private async Task<TaskData> GetTaskDataAsync (Guid taskId)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var taskList = await store.Table<TaskData> ()
                           .Where (m => m.Id == taskId)
                           .Take (1)
                           .ToListAsync ();
            return taskList.First ();
        }

        private async Task<ClientData> GetClientDataAsync (Guid clientId)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var clientList = await store.Table<ClientData> ()
                             .Where (m => m.Id == clientId)
                             .Take (1)
                             .ToListAsync ();
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

        private async Task<ClientData> UpdateClient (ProjectData newProjectData, ClientData oldClientData)
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
}

