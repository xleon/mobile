using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using System.Threading.Tasks;

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

            // TODO: Really should use async here
            timeEntry = new TimeEntryModel (TimeEntryId);
            workspace = new WorkspaceModel (WorkspaceId);
            Task.WhenAll (timeEntry.LoadAsync (), workspace.LoadAsync ()).Wait ();

            // TODO: Determine if timeEntry or workspace is deleted and dismiss dialog
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
            // TODO: Remove workaround after support library upgrade!
            // base.OnStart ();
            Android.Runtime.JNIEnv.CallNonvirtualVoidMethod (Handle, ThresholdClass,
                Android.Runtime.JNIEnv.GetMethodID (ThresholdClass, "onStart", "()V"));
            // End of workaround

            positiveButton = ((AlertDialog)Dialog).GetButton ((int)DialogButtonType.Positive);
            SyncButton ();
        }

        private void OnNameEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            SyncButton ();
        }

        private async void OnPositiveButtonClicked (object sender, DialogClickEventArgs e)
        {
            if (isSaving)
                return;

            isSaving = true;
            try {
                if (workspace == null)
                    return;

                var project = new ProjectModel () {
                    Workspace = workspace,
                    Name = nameEditText.Text,
                    Color = ProjectColor,
                    IsActive = true,
                };
                await project.SaveAsync ();

                if (timeEntry != null) {
                    timeEntry.Workspace = project.Workspace;
                    timeEntry.Project = project;
                    timeEntry.Task = null;
                    await timeEntry.SaveAsync ();
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

