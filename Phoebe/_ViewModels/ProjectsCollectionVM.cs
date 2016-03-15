using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Net;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    public class ProjectsCollectionVM : ObservableRangeCollection<CommonData>
    {
        private List<ClientData> clients;
        private List<SuperProjectData> projects;
        private List<TaskData> tasks;
        private SortProjectsBy sortBy;
        private Guid workspaceId;
        private string projectNameFilter;

        public enum SortProjectsBy {
            Projects,
            Clients
        }

        public ProjectsCollectionVM (TimerState timerState, SortProjectsBy sortBy, Guid workspaceId)
        {
            this.sortBy = sortBy;
            this.workspaceId = workspaceId;
            var userData = ServiceContainer.Resolve<AuthManager> ().User;

            clients = timerState.Clients.Values
                      .OrderBy (r => r.Name).ToList ();

            tasks = timerState.Tasks.Values
                    .Where (r => r.IsActive)
                    .OrderBy (r => r.Name).ToList ();

            projects = timerState.GetUserAccessibleProjects (userData.Id).Select (
                           p => new SuperProjectData (
                p,
                clients.FirstOrDefault (c => c.Id == p.ClientId),
                tasks.Count (t => t.ProjectId == p.Id))).ToList ();

            // Create collection
            CreateSortedCollection (projects);
        }

        public void Dispose ()
        {
        }

        #region List operations
        public SortProjectsBy SortBy
        {
            get {
                return sortBy;
            } set {
                if (sortBy == value) {
                    return;
                }
                sortBy = value;
                CreateSortedCollection (projects);
            }
        }

        public Guid WorkspaceId
        {
            get {
                return workspaceId;
            } set {
                if (workspaceId == value) {
                    return;
                }
                workspaceId = value;
                CreateSortedCollection (projects);
            }
        }

        public string ProjectNameFilter
        {
            get {
                return projectNameFilter;
            } set {
                if (projectNameFilter == value) {
                    return;
                }
                projectNameFilter = value;
                var prjs = string.IsNullOrEmpty (value) ? projects : projects.Where (p => p.Name.ToLower ().Contains (projectNameFilter.ToLower ()));
                CreateSortedCollection (prjs);
            }
        }
        #endregion

        private void CreateSortedCollection (IEnumerable<SuperProjectData> projectList)
        {
            var enumerable = projectList as IList<SuperProjectData> ?? projectList.ToList ();
            var data = new List<CommonData> ();

            // TODO: Maybe group using linq is clearer
            if (sortBy == SortProjectsBy.Clients) {

                // Add section without client
                data.Add (new ClientData ());
                data.Add (GetEmptyProject ());
                enumerable.Where (p => p.ClientId == null && p.WorkspaceId == workspaceId).ForEach (data.Add);

                // Add normal sections
                var sectionHeaders = clients.Where (p => p.WorkspaceId == workspaceId);
                foreach (var header in sectionHeaders) {
                    var sectionItems = enumerable.Where (p => p.ClientId == header.Id && p.WorkspaceId == workspaceId).ToList ();
                    if (sectionItems.Count > 0) {
                        data.Add (header);
                        sectionItems.ForEach (data.Add);
                    }
                }
            } else {
                data.Add (GetEmptyProject ());
                enumerable.Where (p => p.WorkspaceId == workspaceId).ForEach (data.Add);
            }

            // ObservableRange method :)
            Reset (data);
        }

        public void AddTasks (ProjectData project)
        {
            // Remove previous tasks
            var oldTaskIndex = this.IndexOf (p => p is TaskData);
            if (oldTaskIndex != -1) {
                RemoveRange (this.OfType<TaskData> ());
            }

            // Insert new tasks
            var newTaskIndex = this.IndexOf (p => p.Id == project.Id) + 1;
            if (oldTaskIndex != newTaskIndex) {
                InsertRange (tasks.Where (p => p.ProjectId == project.Id), newTaskIndex);
            }
        }

        public IEnumerable<CommonData> Data
        {
            get {
                return this;
            }
        }

        public class SuperProjectData : ProjectData
        {
            public string ClientName { get; private set; }
            public int TaskNumber { get; private set; }
            public bool IsEmpty { get; private set; }

            public SuperProjectData (ProjectData dataObject,
                                     ClientData client, int taskNumber, bool isEmpty = false) : base (dataObject)
            {
                TaskNumber = taskNumber;
                IsEmpty = isEmpty;
                ClientName = client != null ? client.Name : "";
            }
        }

        private SuperProjectData GetEmptyProject ()
        {
            return new SuperProjectData (new ProjectData (), null, 0, true);
        }
    }
}
