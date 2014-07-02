using System;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Data.Views;

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

        private Guid TimeEntryId {
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

                if (m is TaskModel) {
                    task = (TaskModel)m;
                    project = task != null ? task.Project : null;
                    workspace = project != null ? project.Workspace : null;
                } else if (m is ProjectAndTaskView.Project) {
                    var wrap = (ProjectAndTaskView.Project)m;
                    if (wrap.IsNoProject) {
                        workspace = new WorkspaceModel (wrap.WorkspaceId);
                    } else if (wrap.IsNewProject) {
                        var proj = (ProjectModel)wrap.Data;
                        // Show create project dialog instead
                        new CreateProjectDialogFragment (model, proj.Workspace, proj.Color)
                            .Show (FragmentManager, "new_project_dialog");
                    } else {
                        project = (ProjectModel)wrap.Data;
                        workspace = project != null ? project.Workspace : null;
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

            Dismiss ();
        }
    }
}
