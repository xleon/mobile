using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class CreateTagDialogFragment : BaseDialogFragment
    {
        private static readonly string WorkspaceIdArgument = "com.toggl.timer.workspace_id";
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        private EditText nameEditText;
        private Button positiveButton;

        public CreateTagDialogFragment (Guid workspaceId, TimeEntryModel timeEntry)
        {
            var args = new Bundle ();

            args.PutString (WorkspaceIdArgument, workspaceId.ToString ());
            if (timeEntry != null) {
                args.PutString (TimeEntryIdArgument, timeEntry.Id.ToString ());
            }

            Arguments = args;
        }

        public CreateTagDialogFragment ()
        {
        }

        public CreateTagDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
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


        private WorkspaceModel workspace;
        private TimeEntryModel timeEntry;
        private bool modelsLoaded;

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            LoadData ();
        }

        private async void LoadData ()
        {
            workspace = new WorkspaceModel (WorkspaceId);
            if (TimeEntryId != Guid.Empty) {
                timeEntry = new TimeEntryModel (TimeEntryId);
                await Task.WhenAll (workspace.LoadAsync (), timeEntry.LoadAsync ());
            } else {
                await workspace.LoadAsync ();
            }

            modelsLoaded = true;
            ValidateTagName ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            nameEditText = new EditText (Activity);
            nameEditText.SetHint (Resource.String.CreateTagDialogHint);
            nameEditText.InputType = InputTypes.TextFlagCapSentences;
            nameEditText.TextChanged += OnNameEditTextTextChanged;

            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.CreateTagDialogTitle)
                   .SetView (nameEditText)
                   .SetPositiveButton (Resource.String.CreateTagDialogOk, OnPositiveButtonClicked)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();
            positiveButton = ((AlertDialog)Dialog).GetButton ((int)DialogButtonType.Positive);
            ValidateTagName ();

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Project";
        }

        private void OnNameEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            ValidateTagName ();
        }

        private async void OnPositiveButtonClicked (object sender, DialogClickEventArgs e)
        {
            if (!modelsLoaded) {
                return;
            }

            if (workspace == null) {
                return;
            }

            await AssignTag (workspace, nameEditText.Text, timeEntry);
        }

        private static async Task AssignTag (WorkspaceModel workspace, string tagName, TimeEntryModel timeEntry)
        {
            var store = ServiceContainer.Resolve<IDataStore>();
            var existing = await store.Table<TagData>()
                           .QueryAsync (r => r.WorkspaceId == workspace.Id && r.Name == tagName)
                           .ConfigureAwait (false);

            var checkRelation = true;
            TagModel tag;
            if (existing.Count > 0) {
                tag = new TagModel (existing [0]);
            } else {
                tag = new TagModel () {
                    Name = tagName,
                    Workspace = workspace,
                };
                await tag.SaveAsync ().ConfigureAwait (false);

                checkRelation = false;
            }

            if (timeEntry != null) {
                var assignTag = true;

                if (checkRelation) {
                    // Check if the relation already exists before adding it
                    var relations = await store.Table<TimeEntryTagData> ()
                                    .CountAsync (r => r.TimeEntryId == timeEntry.Id && r.TagId == tag.Id && r.DeletedAt == null)
                                    .ConfigureAwait (false);
                    if (relations < 1) {
                        assignTag = false;
                    }
                }

                if (assignTag) {
                    var relationModel = new TimeEntryTagModel () {
                        TimeEntry = timeEntry,
                        Tag = tag,
                    };
                    await relationModel.SaveAsync ().ConfigureAwait (false);

                    timeEntry.Touch ();
                    await timeEntry.SaveAsync ().ConfigureAwait (false);
                }
            }
        }

        private void ValidateTagName ()
        {
            if (positiveButton == null || nameEditText == null) {
                return;
            }

            var valid = true;
            var name = nameEditText.Text;

            if (!modelsLoaded) {
                valid = false;
            } else if (String.IsNullOrWhiteSpace (name)) {
                valid = false;
            }

            positiveButton.Enabled = valid;
        }
    }
}
