using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey.UI.Fragments
{
    public class CreateProjectDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        private static readonly string WorkspaceIdArgument = "com.toggl.timer.workspace_id";
        private static readonly string ProjectColorArgument = "com.toggl.timer.project_color";
        private EditText nameEditText;
        private Button positiveButton;

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

        private Guid TimeEntryId {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        private Guid WorkspaceId {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (WorkspaceIdArgument), out id);
                }
                return id;
            }
        }

        private int ProjectColor {
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

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            timeEntry = Model.ById<TimeEntryModel> (TimeEntryId);
            workspace = Model.ById<WorkspaceModel> (WorkspaceId);
            if (workspace == null) {
                Dismiss ();
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
        }

        private void OnNameEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            SyncButton ();
        }

        private void OnPositiveButtonClicked (object sender, DialogClickEventArgs e)
        {
            if (workspace == null)
                return;

            var project = Model.Update (new ProjectModel () {
                Workspace = workspace,
                Name = nameEditText.Text,
                Color = ProjectColor,
                IsActive = true,
                IsPersisted = true,
            });

            if (timeEntry != null) {
                timeEntry.Workspace = project.Workspace;
                timeEntry.Project = project;
                timeEntry.Task = null;
            }
        }

        private void SyncButton ()
        {
            positiveButton.Enabled = !String.IsNullOrWhiteSpace (nameEditText.Text);
        }
    }
}

