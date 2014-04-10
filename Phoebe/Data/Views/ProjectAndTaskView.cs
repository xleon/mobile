using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public class ProjectAndTaskView : IDataView<object>, IDisposable
    {
        private readonly List<Workspace> data = new List<Workspace> ();
        private Subscription<ModelChangedMessage> subscriptionModelChanged;

        public ProjectAndTaskView ()
        {
            Reload ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        public void Dispose ()
        {
            if (subscriptionModelChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (msg.Model is UserModel
                && msg.PropertyName == UserModel.PropertyDefaultWorkspaceId) {

                SortWorkspaces (data);
                OnUpdated ();
            } else if (msg.Model is WorkspaceModel) {
                if (msg.PropertyName == WorkspaceModel.PropertyIsShared
                    || msg.PropertyName == WorkspaceModel.PropertyDeletedAt) {

                    var model = (WorkspaceModel)msg.Model;
                    if (!model.IsShared || model.DeletedAt != null) {
                        // Make sure this workspace is removed:
                        if (data.RemoveAll (ws => ws.Model == model) > 0) {
                            OnUpdated ();
                        }
                    }
                }
            } else if (msg.Model is ProjectModel) {
                if (msg.PropertyName == WorkspaceModel.PropertyIsShared
                    || msg.PropertyName == WorkspaceModel.PropertyDeletedAt) {

                    var model = (ProjectModel)msg.Model;
                    if (!model.IsShared || model.DeletedAt != null) {
                        // Make sure this project is removed:
                        var removals = 0;
                        foreach (var ws in data) {
                            removals += ws.Projects.RemoveAll (p => p.Model == model);
                        }
                        if (removals > 0) {
                            OnUpdated ();
                        }
                    }
                }
            } else if (msg.Model is TaskModel) {
                if (msg.PropertyName == WorkspaceModel.PropertyIsShared
                    || msg.PropertyName == WorkspaceModel.PropertyDeletedAt) {

                    var model = (TaskModel)msg.Model;
                    if (!model.IsShared || model.DeletedAt != null) {
                        // Make sure this task is removed:
                        var removals = 0;
                        foreach (var proj in data.SelectMany(ws => ws.Projects)) {
                            removals += proj.Tasks.RemoveAll (t => t == model);
                        }
                        if (removals > 0) {
                            OnUpdated ();
                        }
                    }
                }
            }
        }

        public event EventHandler Updated;

        private void OnUpdated ()
        {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public async void Reload ()
        {
            if (IsLoading)
                return;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            var shouldSubscribe = subscriptionModelChanged != null;

            if (subscriptionModelChanged != null) {
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
                shouldSubscribe = true;
            }

            IsLoading = true;
            data.Clear ();
            OnUpdated ();

            try {
                data.AddRange (await LoadDataAsync ());
            } finally {
                IsLoading = false;
                OnUpdated ();
                if (shouldSubscribe) {
                    subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
                }
            }
        }

        private static Task<IEnumerable<Workspace>> LoadDataAsync ()
        {
            return Task.Factory.StartNew (() => {
                // Get all workspaces
                var data = Model.Query<WorkspaceModel> ()
                    .NotDeleted ()
                    .Select ((m) => new Workspace (m))
                    .ToList ();

                // Load projects
                var user = ServiceContainer.Resolve<AuthManager> ().User;
                if (user != null) {
                    foreach (var proj in user.GetAllAvailableProjects()) {
                        var ws = data.FirstOrDefault (m => m.Model.Id == proj.WorkspaceId);
                        if (ws == null)
                            continue;

                        ws.Projects.Add (new Project (proj));
                    }
                }

                // Load tasks
                var tasks = Model.Query<TaskModel> (m => m.IsActive)
                    .NotDeleted ();
                foreach (var task in tasks) {
                    var proj = data.SelectMany (ws => ws.Projects)
                        .Where (m => m.Model != null)
                        .FirstOrDefault (m => m.Model.Id == task.ProjectId);
                    if (proj == null)
                        continue;

                    proj.Tasks.Add (task);
                }

                // Sort everything:
                SortWorkspaces (data);
                foreach (var ws in data) {
                    ws.SortProjects ();
                    foreach (var proj in ws.Projects) {
                        proj.SortTasks ();
                    }
                }

                return (IEnumerable<Workspace>)data;
            });
        }

        private static void SortWorkspaces (List<Workspace> data)
        {
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            data.Sort ((a, b) => {
                if (user != null) {
                    if (a.Model != null && a.Model.Id == user.DefaultWorkspaceId) {
                        return -1;
                    }
                    if (b.Model != null && b.Model.Id == user.DefaultWorkspaceId) {
                        return 1;
                    }
                }

                var aName = a.Model != null ? (a.Model.Name ?? String.Empty) : String.Empty;
                var bName = b.Model != null ? (b.Model.Name ?? String.Empty) : String.Empty;
                return String.Compare (aName, bName, StringComparison.Ordinal);
            });
        }

        public void LoadMore ()
        {
        }

        public IEnumerable<object> Data {
            get {
                var includeWorkspaces = data.Count > 1;

                foreach (var ws in data) {
                    if (includeWorkspaces) {
                        yield return ws;
                    }

                    foreach (var proj in ws.Projects) {
                        yield return proj;

                        foreach (var task in proj.Tasks) {
                            yield return task;
                        }
                    }
                }
            }
        }

        public long Count {
            get { return Data.LongCount (); }
        }

        public bool HasMore {
            get { return false; }
        }

        public bool IsLoading { get; private set; }

        public class Workspace
        {
            private readonly WorkspaceModel model;
            private readonly List<Project> projects = new List<Project> ();

            public Workspace (WorkspaceModel model)
            {
                this.model = model;
                projects.Add (new Project (model));
                projects.Add (new Project (new ProjectModel () {
                    Workspace = model,
                    Color = new Random ().Next (),
                }));
            }

            public void SortProjects ()
            {
                projects.Sort ((a, b) => {
                    if (a.IsNoProject != b.IsNoProject) {
                        return a.IsNoProject ? -1 : 1;
                    }

                    if (a.IsNewProject != b.IsNewProject) {
                        return a.IsNewProject ? 1 : -1;
                    }

                    var aName = a.Model != null ? (a.Model.Name ?? String.Empty) : String.Empty;
                    var bName = b.Model != null ? (b.Model.Name ?? String.Empty) : String.Empty;
                    return String.Compare (aName, bName, StringComparison.Ordinal);
                });
            }

            public WorkspaceModel Model {
                get { return model; }
            }

            public List<Project> Projects {
                get { return projects; }
            }
        }

        public class Project
        {
            private readonly ProjectModel model;
            private readonly List<TaskModel> tasks = new List<TaskModel> ();
            private readonly WorkspaceModel workspaceModel;

            public Project (ProjectModel model)
            {
                this.model = model;
                workspaceModel = null;
            }

            public Project (WorkspaceModel model)
            {
                model = null;
                workspaceModel = model;
            }

            public void SortTasks ()
            {
                tasks.Sort ((a, b) => String.Compare (
                    a.Name ?? String.Empty,
                    b.Name ?? String.Empty,
                    StringComparison.Ordinal
                ));
            }

            public bool IsNoProject {
                get { return model == null; }
            }

            public bool IsNewProject {
                get { return model != null && !model.IsShared; }
            }

            public WorkspaceModel WorkspaceModel {
                get { return model != null ? model.Workspace : WorkspaceModel; }
            }

            public ProjectModel Model {
                get { return model; }
            }

            public List<TaskModel> Tasks {
                get { return tasks; }
            }
        }
    }
}
