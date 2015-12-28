using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public class ProjectsCollection : ObservableRangeCollection<CommonData>, ICollectionData<CommonData>
    {
        private List<ClientData> clients;
        private List<SuperProjectData> projects;
        private List<TaskData> tasks;
        private SortProjectsBy sortBy;
        private Guid workspaceId;

        public enum SortProjectsBy {
            Projects,
            Clients
        }

        ProjectsCollection (SortProjectsBy sortBy, Guid workspaceId)
        {
            this.sortBy = sortBy;
            this.workspaceId = workspaceId;
        }

        public void Dispose ()
        {
        }

        public static async Task<ProjectsCollection> Init (SortProjectsBy sortBy, Guid workspaceId)
        {
            var v = new ProjectsCollection (sortBy, workspaceId);
            var store = ServiceContainer.Resolve<IDataStore> ();
            var userData = ServiceContainer.Resolve<AuthManager> ().User;

            var projectsTask = store.GetUserAccessibleProjects (userData.Id);
            var clientsTask = store.Table<ClientData> ().Where (r => r.DeletedAt == null)
                              .OrderBy (r => r.Name).ToListAsync ();
            var tasksTask = store.Table<TaskData> ().Where (r => r.DeletedAt == null && r.IsActive)
                            .OrderBy (r => r.Name).ToListAsync();
            await Task.WhenAll (projectsTask, tasksTask, clientsTask);

            v.clients = clientsTask.Result;
            v.tasks = tasksTask.Result;
            v.projects = projectsTask.Result.Select (p =>
                         new SuperProjectData (p,
                                               v.clients.FirstOrDefault (c => c.Id == p.ClientId),
                                               v.tasks.Count (t => t.ProjectId == p.Id))
                                                    ).ToList ();

            // Create collection
            v.CreateSortedCollection ();
            return v;
        }

        public SortProjectsBy SortBy
        {
            get {
                return sortBy;
            } set {
                if (sortBy == value) {
                    return;
                }
                sortBy = value;
                CreateSortedCollection ();
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
                CreateSortedCollection ();
            }
        }

        private void CreateSortedCollection ()
        {
            // Reset collection.
            Clear ();

            // TODO: Try with a grouping method
            // using linq

            if (sortBy == SortProjectsBy.Clients) {

                // Add no client section
                Add (new ClientData ());
                Add (GetEmptyProject ());
                AddRange (projects.Where (p => p.ClientId == null && p.WorkspaceId == workspaceId));

                // Add normal sections
                var filteredClients = clients.Where (p => p.WorkspaceId == workspaceId);
                foreach (var item in filteredClients) {
                    var filteredProjects = projects.Where (p => p.ClientId == item.Id && p.WorkspaceId == workspaceId).ToList ();
                    if (filteredProjects.Count > 0) {
                        Add (item);
                        AddRange (filteredProjects);
                    }
                }
            } else {
                Add (GetEmptyProject ());
                AddRange (projects.Where (p => p.WorkspaceId == workspaceId));
            }
        }

        public void AddTasks (ProjectData project)
        {
            RemoveTasks ();
            var index = this.IndexOf (p => p.Id == project.Id);
            InsertRange (tasks.Where (p => p.ProjectId == project.Id), index);
        }

        public void RemoveTasks ()
        {
            RemoveRange (this.OfType<TaskData> ());
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
