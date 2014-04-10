using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Adapters
{
    public class ProjectsAdapter : BaseDataViewAdapter<object>
    {
        protected static readonly int ViewTypeWorkspace = ViewTypeContent;
        protected static readonly int ViewTypeNoProject = ViewTypeContent + 1;
        protected static readonly int ViewTypeProject = ViewTypeContent + 2;
        protected static readonly int ViewTypeNewProject = ViewTypeContent + 3;
        protected static readonly int ViewTypeTask = ViewTypeContent + 4;
        private readonly ExpandableProjectsView dataView;

        public ProjectsAdapter () : this (new ExpandableProjectsView ())
        {
        }

        private ProjectsAdapter (ExpandableProjectsView dataView) : base (dataView)
        {
            this.dataView = dataView;
        }

        public override int ViewTypeCount {
            get { return base.ViewTypeCount + 4; }
        }

        public override int GetItemViewType (int position)
        {
            if (position == DataView.Count && DataView.IsLoading)
                return ViewTypeLoaderPlaceholder;

            if (position < 0 || position >= DataView.Count)
                throw new ArgumentOutOfRangeException ("position");

            var obj = DataView.Data.ElementAt (position);
            if (obj is ProjectAndTaskView.Project) {
                var p = (ProjectAndTaskView.Project)obj;
                if (p.IsNewProject) {
                    return ViewTypeNewProject;
                }
                if (p.IsNoProject) {
                    return ViewTypeNoProject;
                }
                return ViewTypeProject;
            } else if (obj is TaskModel) {
                return ViewTypeTask;
            } else if (obj is ProjectAndTaskView.Workspace) {
                return ViewTypeWorkspace;
            }

            throw new NotSupportedException ("No view type defined for given object.");
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            var view = convertView;

            var item = GetEntry (position);
            var viewType = GetItemViewType (position);
            if (viewType == ViewTypeWorkspace) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListWorkspaceItem, parent, false);
                    view.Tag = new WorkspaceListItemHolder (view);
                }

                var holder = (WorkspaceListItemHolder)view.Tag;
                holder.Bind ((ProjectAndTaskView.Workspace)item);
            } else if (viewType == ViewTypeProject) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListProjectItem, parent, false);
                    view.Tag = new ProjectListItemHolder (dataView, view);
                }

                var holder = (ProjectListItemHolder)view.Tag;
                holder.Bind ((ProjectAndTaskView.Project)item);
            } else if (viewType == ViewTypeNoProject) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListNoProjectItem, parent, false);
                    view.Tag = new NoProjectListItemHolder (view);
                }

                var holder = (NoProjectListItemHolder)view.Tag;
                holder.Bind ((ProjectAndTaskView.Project)item);
            } else if (viewType == ViewTypeNewProject) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListNewProjectItem, parent, false);
                    view.Tag = new NewProjectListItemHolder (view);
                }

                var holder = (NewProjectListItemHolder)view.Tag;
                holder.Bind ((ProjectAndTaskView.Project)item);
            } else if (viewType == ViewTypeTask) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.ProjectListTaskItem, parent, false);
                    view.Tag = new TaskListItemHolder (view);
                }

                var holder = (TaskListItemHolder)view.Tag;
                holder.Bind ((TaskModel)item);
            } else {
                throw new NotSupportedException ("Got an invalid view type: {0}" + viewType);
            }

            return view;
        }

        public override bool IsEnabled (int position)
        {
            return !(GetEntry (position) is ProjectAndTaskView.Workspace);
        }

        #region View holders

        private class WorkspaceListItemHolder : ModelViewHolder<ProjectAndTaskView.Workspace>
        {
            public TextView WorkspaceTextView { get; private set; }

            private WorkspaceModel Model {
                get {
                    if (DataSource == null)
                        return null;
                    return DataSource.Model;
                }
            }

            public WorkspaceListItemHolder (View root) : base (root)
            {
                WorkspaceTextView = root.FindViewById<TextView> (Resource.Id.WorkspaceTextView).SetFont (Font.RobotoMedium);
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (Model == null)
                    return;

                if (Model == msg.Model) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor
                        || msg.PropertyName == ProjectModel.PropertyClientId)
                        Rebind ();
                }
            }

            protected override void Rebind ()
            {
                if (Model == null)
                    return;

                WorkspaceTextView.Text = (Model.Name ?? String.Empty).ToUpper ();
            }
        }

        private class ProjectListItemHolder : ModelViewHolder<ProjectAndTaskView.Project>
        {
            private readonly ExpandableProjectsView dataView;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public FrameLayout TasksFrameLayout { get; private set; }

            public TextView TasksTextView { get; private set; }

            public ImageView TasksImageView { get; private set; }

            public ProjectListItemHolder (ExpandableProjectsView dataView, View root) : base (root)
            {
                this.dataView = dataView;

                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont (Font.RobotoLight);
                TasksFrameLayout = root.FindViewById<FrameLayout> (Resource.Id.TasksFrameLayout);
                TasksTextView = root.FindViewById<TextView> (Resource.Id.TasksTextView).SetFont (Font.RobotoMedium);
                TasksImageView = root.FindViewById<ImageView> (Resource.Id.TasksImageView);

                TasksFrameLayout.Click += OnTasksFrameLayoutClick;
            }

            private void OnTasksFrameLayoutClick (object sender, EventArgs e)
            {
                if (DataSource == null)
                    return;
                dataView.ToggleProjectTasks (Model);
            }

            private ProjectModel Model {
                get {
                    if (DataSource == null)
                        return null;
                    return DataSource.Model;
                }
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (Model == null)
                    return;

                if (Model == msg.Model) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor
                        || msg.PropertyName == ProjectModel.PropertyClientId)
                        Rebind ();
                }
            }

            protected override void Rebind ()
            {
                if (Model == null) {
                    ColorView.SetBackgroundColor (ColorView.Resources.GetColor (Resource.Color.dark_gray_text));
                    ProjectTextView.SetText (Resource.String.ProjectsNoProject);
                    ClientTextView.Visibility = ViewStates.Gone;
                    TasksFrameLayout.Visibility = ViewStates.Gone;
                    return;
                }

                var color = Color.ParseColor (Model.GetHexColor ());
                ColorView.SetBackgroundColor (color);
                ProjectTextView.Text = Model.Name;
                if (Model.Client != null) {
                    ClientTextView.Text = Model.Client.Name;
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Visibility = ViewStates.Gone;
                }

                TasksFrameLayout.Visibility = DataSource.Tasks.Count == 0 ? ViewStates.Gone : ViewStates.Visible;
                var expanded = dataView.AreProjectTasksVisible (Model);
                TasksTextView.Visibility = expanded ? ViewStates.Invisible : ViewStates.Visible;
                TasksImageView.Visibility = !expanded ? ViewStates.Invisible : ViewStates.Visible;
            }
        }

        private class NoProjectListItemHolder : BindableViewHolder<ProjectAndTaskView.Project>
        {
            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public NoProjectListItemHolder (View root) : base (root)
            {
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
            }

            protected override void Rebind ()
            {
                ColorView.SetBackgroundColor (ColorView.Resources.GetColor (Resource.Color.dark_gray_text));
                ProjectTextView.SetText (Resource.String.ProjectsNoProject);
            }
        }

        private class NewProjectListItemHolder : BindableViewHolder<ProjectAndTaskView.Project>
        {
            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public NewProjectListItemHolder (View root) : base (root)
            {
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
            }

            private ProjectModel Model {
                get {
                    if (DataSource == null)
                        return null;
                    return DataSource.Model;
                }
            }

            protected override void Rebind ()
            {
                var color = Color.ParseColor (Model.GetHexColor ());
                ColorView.SetBackgroundColor (color);
                ProjectTextView.SetText (Resource.String.ProjectsNewProject);
            }
        }

        private class TaskListItemHolder : ModelViewHolder<TaskModel>
        {
            public TextView TaskTextView { get; private set; }

            public TaskModel Model {
                get { return DataSource; }
            }

            public TaskListItemHolder (View root) : base (root)
            {
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView).SetFont (Font.RobotoLight);
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (DataSource == null)
                    return;

                if (DataSource == msg.Model) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor
                        || msg.PropertyName == ProjectModel.PropertyClientId)
                        Rebind ();
                }
            }

            protected override void Rebind ()
            {
                if (DataSource == null)
                    return;

                TaskTextView.Text = DataSource.Name;
            }
        }

        #endregion

        class ExpandableProjectsView : IDataView<object>, IDisposable
        {
            private readonly HashSet<Guid?> expandedProjectIds = new HashSet<Guid?> ();
            private ProjectAndTaskView dataView;

            public ExpandableProjectsView ()
            {
                dataView = new ProjectAndTaskView ();
                dataView.Updated += OnDataViewUpdated;
            }

            public void Dispose ()
            {
                if (dataView != null) {
                    dataView.Dispose ();
                    dataView.Updated -= OnDataViewUpdated;
                    dataView = null;
                }
            }

            private void OnDataViewUpdated (object sender, EventArgs e)
            {
                OnUpdated ();
            }

            public void ToggleProjectTasks (ProjectModel model)
            {
                if (!expandedProjectIds.Remove (model.Id)) {
                    expandedProjectIds.Add (model.Id);
                }
                OnUpdated ();
            }

            public bool AreProjectTasksVisible (ProjectModel model)
            {
                return expandedProjectIds.Contains (model.Id);
            }

            public event EventHandler Updated;

            private void OnUpdated ()
            {
                var handler = Updated;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            }

            public void Reload ()
            {
                if (dataView != null)
                    dataView.Reload ();
            }

            public void LoadMore ()
            {
                if (dataView != null)
                    dataView.LoadMore ();
            }

            public IEnumerable<object> Data {
                get {
                    if (dataView != null) {
                        foreach (var obj in dataView.Data) {
                            var task = obj as TaskModel;
                            if (task != null) {
                                if (!expandedProjectIds.Contains (task.ProjectId))
                                    continue;
                            }
                            yield return obj;
                        }
                    }
                }
            }

            public long Count {
                get { return Data.LongCount (); }
            }

            public bool HasMore {
                get {
                    if (dataView != null)
                        return dataView.HasMore;
                    return false;
                }
            }

            public bool IsLoading {
                get {
                    if (dataView != null)
                        return dataView.IsLoading;
                    return false;
                }
            }
        }
    }
}
