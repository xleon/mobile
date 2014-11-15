using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class CreateProjectDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        private static readonly string WorkspaceIdArgument = "com.toggl.timer.workspace_id";
        private static readonly string ProjectColorArgument = "com.toggl.timer.project_color";
        private EditText nameEditText;
        private Button positiveButton;
        private bool isSaving;

        public CreateProjectDialogFragment (WorkspaceModel workspace, int color) : this (null, workspace, color)
        {
        }

        public CreateProjectDialogFragment (TimeEntryModel timeEntry, WorkspaceModel workspace, int color)
        {
            var args = new Bundle ();

            if (timeEntry != null) {
                args.PutString (TimeEntryIdArgument, timeEntry.Id.ToString ());
            }
            args.PutString (WorkspaceIdArgument, workspace.Id.ToString ());
            args.PutInt (ProjectColorArgument, color);

            Arguments = args;
        }

        public CreateProjectDialogFragment ()
        {
        }

        public CreateProjectDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
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

        private Guid WorkspaceId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (WorkspaceIdArgument), out id);
                }
                return id;
            }
        }

        private int ProjectColor
        {
            get {
                var color = 0;
                if (Arguments != null) {
                    color = Arguments.GetInt (ProjectColorArgument, color);
                }
                return color;
            }
        }

        private TimeEntryModel timeEntry;
        private WorkspaceModel workspace;
        private bool modelsLoaded;

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            LoadData ();
        }

        private async void LoadData ()
        {
            timeEntry = new TimeEntryModel (TimeEntryId);
            workspace = new WorkspaceModel (WorkspaceId);
            await Task.WhenAll (timeEntry.LoadAsync (), workspace.LoadAsync ());

            if (timeEntry.Workspace == null || timeEntry.Workspace.Id == Guid.Empty) {
                // TODO: Better logic to determine if the models are actually non-existent
                Dismiss ();
            } else {
                modelsLoaded = true;
            }
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            nameEditText = new EditText (Activity);
            nameEditText.SetHint (Resource.String.CreateProjectDialogHint);
            nameEditText.InputType = InputTypes.TextFlagCapSentences;
            nameEditText.TextChanged += OnNameEditTextTextChanged;

            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.CreateProjectDialogTitle)
                   .SetView (nameEditText)
                   .SetPositiveButton (Resource.String.CreateProjectDialogOk, OnPositiveButtonClicked)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();
            positiveButton = ((AlertDialog)Dialog).GetButton ((int)DialogButtonType.Positive);
            SyncButton ();

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Project";
        }

        private void OnNameEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            SyncButton ();
        }

        private async void OnPositiveButtonClicked (object sender, DialogClickEventArgs e)
        {
            if (!modelsLoaded || isSaving) {
                return;
            }

            isSaving = true;
            try {
                var workspaceModel = workspace;
                var timeEntryModel = timeEntry;

                if (workspaceModel == null) {
                    return;
                }

                var project = new ProjectModel () {
                    Workspace = workspaceModel,
                    Name = nameEditText.Text,
                    Color = ProjectColor,
                    IsActive = true,
                };
                await project.SaveAsync ();

                if (timeEntryModel != null) {
                    timeEntryModel.Workspace = project.Workspace;
                    timeEntryModel.Project = project;
                    timeEntryModel.Task = null;
                    await timeEntryModel.SaveAsync ();
                }
            } finally {
                isSaving = false;
            }
        }

        private void SyncButton ()
        {
            positiveButton.Enabled = !String.IsNullOrWhiteSpace (nameEditText.Text);
        }
    }
}

