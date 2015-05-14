using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class CreateTagDialogFragment : BaseDialogFragment
    {
        private static readonly string WorkspaceIdArgument = "com.toggl.timer.workspace_id";
        private static readonly string TimeEntriesIdsArgument = "com.toggl.timer.time_entry_ids";

        private EditText nameEditText;
        private Button positiveButton;
        private ITimeEntryModel model;
        private WorkspaceModel workspace;

        public CreateTagDialogFragment (Guid workspaceId, ITimeEntryModel model)
        {
            this.model = model;

            var args = new Bundle ();

            args.PutString (WorkspaceIdArgument, workspaceId.ToString ());
            if (model != null) {
                args.PutStringArrayList (TimeEntriesIdsArgument, model.Ids);
            }

            Arguments = args;
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

        public CreateTagDialogFragment ()
        {
        }

        public CreateTagDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public override async void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            workspace = new WorkspaceModel (WorkspaceId);
            await workspace.LoadAsync ();
            if (model == null) {
                var arrayList = Arguments.GetStringArrayList (TimeEntriesIdsArgument);
                if (arrayList.Count > 1) {
                    model = await TimeEntryGroup.BuildTimeEntryGroupAsync (arrayList);
                } else {
                    model = new TimeEntryModel (arrayList.First());
                }
            }

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
            if (model.Workspace == null) {
                return;
            }

            await AssignTag (workspace, nameEditText.Text, model);
        }

        private static async Task AssignTag (WorkspaceModel workspace, string tagName, ITimeEntryModel model)
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

            if (model != null) {
                await model.Apply (async delegate (TimeEntryModel m) {
                    var assignTag = true;

                    if (checkRelation) {
                        // Check if the relation already exists before adding it
                        var relations = await store.Table<TimeEntryTagData> ()
                                        .CountAsync (r => r.TimeEntryId == m.Id && r.TagId == tag.Id && r.DeletedAt == null)
                                        .ConfigureAwait (false);
                        if (relations < 1) {
                            assignTag = false;
                        }
                    }

                    if (assignTag) {
                        var relationModel = new TimeEntryTagModel () {
                            TimeEntry = m,
                            Tag = tag,
                        };
                        await relationModel.SaveAsync ();
                    }
                });
            }
        }

        private void ValidateTagName ()
        {
            if (positiveButton == null || nameEditText == null) {
                return;
            }

            var valid = true;
            var name = nameEditText.Text;

            if (String.IsNullOrWhiteSpace (name)) {
                valid = false;
            }

            positiveButton.Enabled = valid;
        }
    }
}
