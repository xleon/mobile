using System;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Joey.UI.Adapters;

namespace Toggl.Joey.UI.Fragments
{
    public class ChooseTimeEntryProjectDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        public ChooseTimeEntryProjectDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChooseTimeEntryProjectDialogFragment ()
        {
        }

        public ChooseTimeEntryProjectDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        private Guid TimeEntryId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        private ProjectsAdapter adapter;
        private TimeEntryModel model;
        private bool modelLoaded;

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            LoadData ();
        }

        private async void LoadData ()
        {
            model = new TimeEntryModel (TimeEntryId);
            await model.LoadAsync ();
            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                Dismiss ();
            } else {
                modelLoaded = true;
            }
        }

        public override Dialog OnCreateDialog (Bundle state)
        {
            adapter = new ProjectsAdapter ();
            var dia = new AlertDialog.Builder (Activity)
            .SetTitle (Resource.String.ChooseTimeEntryProjectDialogTitle)
            .SetAdapter (adapter, OnItemSelected)
            .SetNegativeButton (Resource.String.ChooseTimeEntryProjectDialogCancel, OnCancelButtonClicked)
            .Create ();

            dia.ListView.Divider = Activity.Resources.GetDrawable (Resource.Drawable.dividerhorizontalopaque);
            return dia;
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
            Dismiss ();
        }

        private async void OnItemSelected (object sender, DialogClickEventArgs args)
        {
            if (modelLoaded && model != null) {
                var m = adapter.GetEntry (args.Which);

                TaskModel task = null;
                ProjectModel project = null;
                WorkspaceModel workspace = null;

                if (m is TaskData) {
                    task = (TaskModel) (TaskData)m;
                    if (task.Project != null) {
                        await task.Project.LoadAsync ();
                        project = task.Project;
                        workspace = project.Workspace ?? task.Workspace;
                    } else {
                        workspace = task.Workspace;
                    }
                } else if (m is ProjectAndTaskView.Project) {
                    var wrap = (ProjectAndTaskView.Project)m;
                    if (wrap.IsNoProject) {
                        workspace = new WorkspaceModel (wrap.WorkspaceId);
                    } else if (wrap.IsNewProject) {
                        var data = wrap.Data;
                        var ws = new WorkspaceModel (data.WorkspaceId);
                        // Show create project dialog instead
                        new CreateProjectDialogFragment (model, ws, data.Color)
                        .Show (FragmentManager, "new_project_dialog");
                    } else {
                        project = (ProjectModel)wrap.Data;
                        workspace = project.Workspace;
                    }
                } else if (m is ProjectAndTaskView.Workspace) {
                    var wrap = (ProjectAndTaskView.Workspace)m;
                    workspace = (WorkspaceModel)wrap.Data;
                }

                if (project != null || task != null || workspace != null) {
                    model.Workspace = workspace;
                    model.Project = project;
                    model.Task = task;
                    await model.SaveAsync ();
                }
            }
        }
    }
}
