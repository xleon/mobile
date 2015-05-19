using System;
using System.Collections.Specialized;
using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using PopupArgs = Android.Widget.PopupMenu.MenuItemClickEventArgs;
using Android.App;
using Toggl.Joey.UI.Activities;

namespace Toggl.Joey.UI.Adapters
{
    public class ProjectListAdapter : RecycledDataViewAdapter<object>
    {
        protected static readonly int ViewTypeContent = 1;
        protected static readonly int ViewTypeWorkspace = ViewTypeContent;
        protected static readonly int ViewTypeNoProject = ViewTypeContent + 1;
        protected static readonly int ViewTypeProject = ViewTypeContent + 2;
        protected static readonly int ViewTypeNewProject = ViewTypeContent + 3;
        protected static readonly int ViewTypeLoaderPlaceholder = 0;

        public Action<object> HandleProjectSelection { get; set; }

        public ProjectListAdapter (RecyclerView owner) : base (owner, new ProjectListView())
        {
        }

        protected override void CollectionChanged (NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                NotifyDataSetChanged();
            }
        }

        protected override RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType)
        {
            View view;
            RecyclerView.ViewHolder holder;

            if (viewType == ViewTypeWorkspace) {
                // header
                view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.ProjectListWorkspaceItem, parent, false);
                holder = new WorkspaceListItemHolder (view);
            } else {
                // projects
                if (viewType == ViewTypeProject) {
                    view =  LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.ProjectListProjectItem, parent, false);
                    holder = new ProjectListItemHolder (this, view);
                } else if (viewType == ViewTypeNewProject) {
                    view =  LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.ProjectListNewProjectItem, parent, false);
                    holder = new NewProjectListItemHolder (this, view);
                } else {
                    view =  LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.ProjectListNoProjectItem, parent, false);
                    holder = new NoProjectListItemHolder (this, view);
                }
            }
            return holder;
        }

        protected override void BindHolder (RecyclerView.ViewHolder holder, int position)
        {
            var viewType = GetItemViewType (position);

            if (viewType == ViewTypeWorkspace) {
                var workspaceHolder = (WorkspaceListItemHolder)holder;
                workspaceHolder.Bind ((ProjectListView.Workspace) GetEntry (position));
            } else {
                var data = (ProjectListView.Project) GetEntry (position);
                if (viewType == ViewTypeProject) {
                    var projectHolder = (ProjectListItemHolder)holder;
                    projectHolder.Bind (data);
                } else if (viewType == ViewTypeNewProject) {
                    var projectHolder = (NewProjectListItemHolder)holder;
                    projectHolder.Bind (data);
                } else {
                    var projectHolder = (NoProjectListItemHolder)holder;
                    projectHolder.Bind (data);
                }
            }
        }

        public override int GetItemViewType (int position)
        {
            if (position == DataView.Count) {
                return ViewTypeLoaderPlaceholder;
            }

            var obj = GetEntry (position);
            if (obj is ProjectListView.Project) {
                var p = (ProjectListView.Project)obj;
                if (p.IsNewProject) {
                    return ViewTypeNewProject;
                }

                return p.IsNoProject ? ViewTypeNoProject : ViewTypeProject;
            }

            return ViewTypeWorkspace;
        }

        private void OnDeleteTimeEntry (object item)
        {
            var handler = HandleProjectSelection;
            if (handler != null) {
                handler (item);
            }
        }

        #region View holders

        public class WorkspaceListItemHolder : RecycledModelViewHolder<ProjectListView.Workspace>
        {
            private WorkspaceModel model;

            public TextView WorkspaceTextView { get; private set; }

            protected override void OnDataSourceChanged ()
            {
                model = null;
                if (DataSource != null) {
                    model = (WorkspaceModel)DataSource.Data;
                }

                base.OnDataSourceChanged ();
            }

            public WorkspaceListItemHolder (View root) : base (root)
            {
                WorkspaceTextView = root.FindViewById<TextView> (Resource.Id.WorkspaceTextView).SetFont (Font.RobotoMedium);
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (model != null) {
                    Tracker.Add (model, HandleWorkspacePropertyChanged);
                }

                Tracker.ClearStale ();
            }

            private void HandleWorkspacePropertyChanged (string prop)
            {
                if (prop == WorkspaceModel.PropertyName) {
                    Rebind ();
                }
            }

            protected override void Rebind ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }
                ResetTrackedObservables ();
                if (model == null) {
                    return;
                }

                WorkspaceTextView.Text = (model.Name ?? String.Empty);
            }
        }

        public class ProjectListItemHolder : RecycledModelViewHolder<ProjectListView.Project>, View.IOnClickListener
        {
            private ProjectModel model;
            private readonly ProjectListAdapter adapter;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public FrameLayout TasksFrameLayout { get; private set; }

            public TextView TasksTextView { get; private set; }

            public ImageView TasksImageView { get; private set; }

            public ProjectListItemHolder (ProjectListAdapter adapter, View root) : base (root)
            {
                this.adapter = adapter;
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont (Font.RobotoLight);
                TasksFrameLayout = root.FindViewById<FrameLayout> (Resource.Id.TasksFrameLayout);
                TasksTextView = root.FindViewById<TextView> (Resource.Id.TasksTextView).SetFont (Font.RobotoMedium);
                TasksImageView = root.FindViewById<ImageView> (Resource.Id.TasksImageView);

                root.SetOnClickListener (this);
            }

            protected override void OnDataSourceChanged ()
            {
                model = null;
                if (DataSource != null) {
                    model = (ProjectModel)DataSource.Data;
                }

                base.OnDataSourceChanged ();
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (model != null) {
                    Tracker.Add (model, HandleProjectPropertyChanged);

                    if (model.Client != null) {
                        Tracker.Add (model.Client, HandleClientPropertyChanged);
                    }
                }

                Tracker.ClearStale ();
            }

            private void HandleProjectPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyClient
                        || prop == ProjectModel.PropertyColor
                        || prop == ProjectModel.PropertyName) {
                    Rebind ();
                }
            }

            private void HandleClientPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyName) {
                    Rebind ();
                }
            }

            protected override void Rebind ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                ResetTrackedObservables ();

                if (model == null) {
                    ColorView.SetBackgroundColor (ColorView.Resources.GetColor (Resource.Color.dark_gray_text));
                    ProjectTextView.SetText (Resource.String.ProjectsNoProject);
                    ClientTextView.Visibility = ViewStates.Gone;
                    TasksFrameLayout.Visibility = ViewStates.Gone;
                    return;
                }

                var color = Color.ParseColor (model.GetHexColor ());
                ColorView.SetBackgroundColor (color);
                ProjectTextView.SetTextColor (color);
                ClientTextView.SetTextColor (color);

                ProjectTextView.Text = model.Name;
                if (model.Client != null) {
                    ClientTextView.Text = model.Client.Name;
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Visibility = ViewStates.Gone;
                }

                TasksFrameLayout.Visibility = ViewStates.Gone;
            }

            public void OnClick (View v)
            {
                adapter.HandleProjectSelection (DataSource);
            }
        }

        public class NoProjectListItemHolder : RecycledBindableViewHolder<ProjectListView.Project>, View.IOnClickListener
        {
            private readonly ProjectListAdapter adapter;

            public TextView ProjectTextView { get; private set; }

            public NoProjectListItemHolder (ProjectListAdapter adapter, View root) : base (root)
            {
                this.adapter = adapter;
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
                root.SetOnClickListener (this);
            }

            protected override void Rebind ()
            {
                ProjectTextView.SetText (Resource.String.ProjectsNoProject);
            }

            public void OnClick (View v)
            {
                adapter.HandleProjectSelection (DataSource);
            }
        }

        private class NewProjectListItemHolder : RecycledBindableViewHolder<ProjectListView.Project>, View.IOnClickListener
        {
            private readonly ProjectListAdapter adapter;

            public TextView ProjectTextView { get; private set; }

            public NewProjectListItemHolder (ProjectListAdapter adapter, View root) : base (root)
            {
                this.adapter = adapter;
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
                root.SetOnClickListener (this);
            }

            private ProjectModel model;

            protected override void OnDataSourceChanged ()
            {
                model = null;
                if (DataSource != null && DataSource.Data != null) {
                    model = new ProjectModel (DataSource.Data);
                }

                base.OnDataSourceChanged ();
            }

            protected override void Rebind ()
            {
                var color = Color.ParseColor (model.GetHexColor ());
                ProjectTextView.SetText (Resource.String.ProjectsNewProject);
            }

            public void OnClick (View v)
            {
                adapter.HandleProjectSelection (DataSource);
            }
        }
        #endregion
    }
}

