using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public class ProjectsAdapter : BaseAdapter
    {
        protected static readonly int ViewTypeWorkspace = 0;
        protected static readonly int ViewTypeProject = 1;
        protected static readonly int ViewTypeTask = 2;
        #pragma warning disable 0414
        private readonly object subscriptionModelChanged;
        #pragma warning restore 0414
        private readonly List<object> data = new List<object> ();
        private readonly List<WorkspaceWrapper> workspaces = new List<WorkspaceWrapper> ();
        private bool dataStale = true;
        private bool workspacesStale = true;

        public ProjectsAdapter () : base ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        private void EnsureWorkspaces ()
        {
            if (!workspacesStale)
                return;

            workspaces.Clear ();
            workspaces.AddRange (Model.Query<WorkspaceModel> ()
                .NotDeleted ()
                .Select ((m) => new WorkspaceWrapper (this, m)));
            workspaces.Sort (WorkspaceComparison);

            workspacesStale = false;
        }

        private void EnsureData ()
        {
            if (!dataStale)
                return;

            EnsureWorkspaces ();

            // Flatten data:
            data.Clear ();

            // If there are more than 1 visible workspace, we need to display workspaces
            var addWorkspaces = workspaces.Where ((m) => m.ProjectCount > 0).Count () > 1;

            foreach (var workspace in workspaces) {
                if (addWorkspaces) {
                    data.Add (workspace);
                }

                foreach (var projects in workspace.Projects) {
                    data.Add (projects);

                    if (projects.IsExpanded) {
                        foreach (var task in projects.Tasks) {
                            data.Add (task);
                        }
                    }
                }
            }

            dataStale = false;
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            EnsureData ();
            var view = convertView;

            var viewType = GetItemViewType (position);
            if (viewType == ViewTypeWorkspace) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListWorkspaceItem, parent, false);
                    view.Tag = new WorkspaceListItemHolder (view);
                }

                var holder = (WorkspaceListItemHolder)view.Tag;
                holder.Bind ((WorkspaceWrapper)data [position]);
            } else if (viewType == ViewTypeProject) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListProjectItem, parent, false);
                    view.Tag = new ProjectListItemHolder (view);
                }

                var holder = (ProjectListItemHolder)view.Tag;
                holder.Bind ((ProjectWrapper)data [position]);
            } else if (viewType == ViewTypeTask) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListTaskItem, parent, false);
                    view.Tag = new TaskListItemHolder (view);
                }

                var holder = (TaskListItemHolder)view.Tag;
                holder.Bind ((TaskModel)data [position]);
            } else {
                throw new NotSupportedException ("Got an invalid view type: {0}" + viewType);
            }

            return view;
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero)
                return;

            if (workspacesStale)
                return;

            if (msg.Model is UserModel
                && msg.PropertyName == UserModel.PropertyDefaultWorkspaceId) {
                workspaces.Sort (WorkspaceComparison);
                Rebind ();
            } else if (msg.Model is WorkspaceModel) {
                if (msg.PropertyName == WorkspaceModel.PropertyName
                    || msg.PropertyName == WorkspaceModel.PropertyIsShared
                    || msg.PropertyName == WorkspaceModel.PropertyDeletedAt) {

                    var model = (WorkspaceModel)msg.Model;
                    if (model.IsShared && model.DeletedAt != null) {
                        if (!workspaces.Any ((w) => w.Model == model)) {
                            workspacesStale = true;
                        } else {
                            workspaces.Sort (WorkspaceComparison);
                            Rebind ();
                        }
                    } else {
                        if (workspaces.Any ((w) => w.Model == model)) {
                            workspacesStale = true;
                        }
                    }
                }
            }

            if (workspacesStale) {
                Rebind ();
            }
        }

        private int WorkspaceComparison (WorkspaceWrapper a, WorkspaceWrapper b)
        {
            // Make sure the default workspace is first:
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            if (user != null) {
                if (a.Model.Id == user.DefaultWorkspaceId) {
                    return -1;
                } else if (b.Model.Id == user.DefaultWorkspaceId) {
                    return 1;
                }
            }

            return a.Model.Name.CompareTo (b.Model.Name);
        }

        public override int GetItemViewType (int position)
        {
            EnsureData ();
            if (position < 0 || position >= data.Count)
                throw new ArgumentOutOfRangeException ("position");

            var obj = data [position];
            if (obj is ProjectWrapper) {
                return ViewTypeProject;
            } else if (obj is TaskModel) {
                return ViewTypeTask;
            } else if (obj is WorkspaceWrapper) {
                return ViewTypeWorkspace;
            }

            throw new NotSupportedException ("No view type defined for given object.");
        }

        public Model GetModel (int position)
        {
            EnsureData ();
            if (position < 0 || position >= data.Count)
                return null;

            var obj = data [position];

            var project = obj as ProjectWrapper;
            if (project != null)
                return project.Model;

            var task = obj as TaskModel;
            if (task != null)
                return task;

            var workspace = obj as WorkspaceWrapper;
            if (workspace != null)
                return workspace.Model;

            return null;
        }

        public override int ViewTypeCount {
            get { return 3; }
        }

        public override int Count {
            get {
                EnsureData ();
                return data.Count;
            }
        }

        private void Rebind ()
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero)
                return;

            dataStale = true;
            NotifyDataSetInvalidated ();
        }

        private class WorkspaceWrapper
        {
            private readonly ProjectsAdapter adapter;
            private readonly WorkspaceModel model;
            private readonly List<ProjectWrapper> projects = new List<ProjectWrapper> ();
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private bool projectsStale = true;

            public WorkspaceWrapper (ProjectsAdapter adapter, WorkspaceModel model)
            {
                this.adapter = adapter;
                this.model = model;

                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            public WorkspaceModel Model {
                get { return model; }
            }

            public void EnsureProjects ()
            {
                if (!projectsStale)
                    return;

                var user = ServiceContainer.Resolve<AuthManager> ().User;

                projects.Clear ();
                if (user != null) {
                    projects.AddRange (user.GetAvailableProjects (model).Select ((m) => new ProjectWrapper (adapter, m)));
                }
                projects.Sort (ProjectComparison);

                projectsStale = false;
            }

            public int ProjectCount {
                get {
                    EnsureProjects ();
                    return projects.Count;
                }
            }

            public IEnumerable<ProjectWrapper> Projects {
                get {
                    EnsureProjects ();
                    return projects;
                }
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                if (projectsStale)
                    return;

                if (msg.Model is ProjectModel) {
                    var project = (ProjectModel)msg.Model;

                    if (projects.Any ((w) => w.Model == project)) {
                        if (msg.PropertyName == ProjectModel.PropertyName) {
                            // Only the name changed, so we can just sort the list again
                            projects.Sort (ProjectComparison);
                            adapter.Rebind ();
                        } else if (msg.PropertyName == ProjectModel.PropertyWorkspaceId
                                   || msg.PropertyName == ProjectModel.PropertyDeletedAt) {
                            // Highly likely that something was removed, invalidate data
                            projectsStale = true;
                        }
                    } else if (msg.PropertyName == ProjectModel.PropertyWorkspaceId
                               || msg.PropertyName == ProjectModel.PropertyDeletedAt
                               || msg.PropertyName == ProjectModel.PropertyIsShared
                               || msg.PropertyName == ProjectModel.PropertyName) {
                        if (project.IsShared && project.DeletedAt == null && project.WorkspaceId == model.Id) {
                            // Something new, maybe it belongs to us?
                            projectsStale = true;
                        }
                    }
                } else if (msg.Model is ProjectUserModel) {
                    var inter = (ProjectUserModel)msg.Model;

                    // Project - user relation changed, need new data
                    if (inter.From.WorkspaceId == model.Id) {
                        projectsStale = true;
                    }
                }

                if (projectsStale) {
                    // Notify the adapter that new stuff is avail
                    adapter.Rebind ();
                }
            }

            private int ProjectComparison (ProjectWrapper a, ProjectWrapper b)
            {
                return (a.Model.Name ?? String.Empty).CompareTo (b.Model.Name ?? String.Empty);
            }
        }

        private class ProjectWrapper
        {
            private readonly ProjectsAdapter adapter;
            private readonly ProjectModel model;
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private readonly List<TaskModel> tasks = new List<TaskModel> ();
            private bool tasksStale = true;
            private bool expanded = false;

            public ProjectWrapper (ProjectsAdapter adapter, ProjectModel model)
            {
                this.adapter = adapter;
                this.model = model;

                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void EnsureTasks ()
            {
                if (!tasksStale)
                    return;

                tasks.Clear ();
                tasks.AddRange (model.Tasks.NotDeleted ().Where ((t) => t.IsActive));
                tasks.Sort (TaskComparison);

                tasksStale = true;
            }

            public ProjectModel Model {
                get { return model; }
            }

            public bool IsExpanded {
                get { return expanded; }
                set {
                    if (expanded == value)
                        return;
                    expanded = value;
                    adapter.Rebind ();
                }
            }

            public int TaskCount {
                get {
                    EnsureTasks ();
                    return tasks.Count;
                }
            }

            public IEnumerable<TaskModel> Tasks {
                get {
                    EnsureTasks ();
                    return tasks;
                }
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                if (tasksStale)
                    return;

                var task = msg.Model as TaskModel;
                if (task == null)
                    return;

                if (msg.PropertyName == TaskModel.PropertyProjectId
                    || msg.PropertyName == TaskModel.PropertyDeletedAt
                    || msg.PropertyName == TaskModel.PropertyIsShared
                    || msg.PropertyName == TaskModel.PropertyIsActive
                    || msg.PropertyName == TaskModel.PropertyName) {
                    if (task.IsShared && task.DeletedAt == null && task.IsActive && task.ProjectId == model.Id) {
                        if (!tasks.Contains (task)) {
                            tasksStale = true;
                        } else {
                            tasks.Sort (TaskComparison);
                            adapter.Rebind ();
                        }
                    } else {
                        if (tasks.Contains (task)) {
                            tasksStale = true;
                        }
                    }
                }

                if (tasksStale) {
                    adapter.Rebind ();
                }
            }

            private int TaskComparison (TaskModel a, TaskModel b)
            {
                return (a.Name ?? String.Empty).CompareTo (b.Name ?? String.Empty);
            }
        }

        private class WorkspaceListItemHolder : Java.Lang.Object
        {
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private WorkspaceModel model;

            public TextView WorkspaceTextView { get; private set; }

            public WorkspaceListItemHolder (View root)
            {
                FindViews (root);

                // Cannot use model.OnPropertyChanged callback directly as it would most probably leak memory,
                // thus the global ModelChangedMessage is used instead.
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void FindViews (View root)
            {
                WorkspaceTextView = root.FindViewById<TextView> (Resource.Id.WorkspaceTextView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;
                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor
                        || msg.PropertyName == ProjectModel.PropertyClientId)
                        Rebind ();
                }
            }

            public void Bind (WorkspaceWrapper wrapper)
            {
                this.model = wrapper.Model;
                Rebind ();
            }

            private void Rebind ()
            {
                WorkspaceTextView.Text = model.Name;
            }
        }

        private class ProjectListItemHolder : Java.Lang.Object
        {
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private ProjectModel model;
            private ProjectWrapper wrapper;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public FrameLayout TasksFrameLayout { get; private set; }

            public TextView TasksTextView { get; private set; }

            public ImageView TasksImageView { get; private set; }

            public ProjectListItemHolder (View root)
            {
                FindViews (root);

                // Cannot use model.OnPropertyChanged callback directly as it would most probably leak memory,
                // thus the global ModelChangedMessage is used instead.
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void FindViews (View root)
            {
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView);
                TasksFrameLayout = root.FindViewById<FrameLayout> (Resource.Id.TasksFrameLayout);
                TasksTextView = root.FindViewById<TextView> (Resource.Id.TasksTextView);
                TasksImageView = root.FindViewById<ImageView> (Resource.Id.TasksImageView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;
                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor
                        || msg.PropertyName == ProjectModel.PropertyClientId)
                        Rebind ();
                }
            }

            public void Bind (ProjectWrapper wrapper)
            {
                this.wrapper = wrapper;
                this.model = wrapper.Model;
                Rebind ();
            }

            private void Rebind ()
            {
                var color = Color.ParseColor (model.GetHexColor ());
                ColorView.SetBackgroundColor (color);
                ProjectTextView.Text = model.Name;
                if (model.Client != null) {
                    ClientTextView.Text = model.Client.Name;
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Visibility = ViewStates.Gone;
                }

                TasksFrameLayout.Visibility = wrapper.TaskCount == 0 ? ViewStates.Gone : ViewStates.Visible;
                TasksTextView.Visibility = wrapper.IsExpanded ? ViewStates.Invisible : ViewStates.Visible;
                TasksImageView.Visibility = !wrapper.IsExpanded ? ViewStates.Invisible : ViewStates.Visible;
            }
        }

        private class TaskListItemHolder : Java.Lang.Object
        {
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private TaskModel model;

            public TextView TaskTextView { get; private set; }

            public TaskListItemHolder (View root)
            {
                FindViews (root);

                // Cannot use model.OnPropertyChanged callback directly as it would most probably leak memory,
                // thus the global ModelChangedMessage is used instead.
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void FindViews (View root)
            {
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor
                        || msg.PropertyName == ProjectModel.PropertyClientId)
                        Rebind ();
                }
            }

            public void Bind (TaskModel model)
            {
                this.model = model;
                Rebind ();
            }

            private void Rebind ()
            {
                TaskTextView.Text = model.Name;
            }
        }
    }
}
