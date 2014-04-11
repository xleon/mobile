using System;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class EditTimeEntryFragment : Fragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        private readonly Handler handler = new Handler ();
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private TimeEntryModel model;
        private bool canRebind;
        private bool descriptionChanging;
        private bool autoCommitScheduled;

        public EditTimeEntryFragment ()
        {
        }

        public EditTimeEntryFragment (TimeEntryModel model)
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public EditTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
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

        protected TimeEntryModel TimeEntry {
            get { return model; }
            set {
                DiscardDescriptionChanges ();
                model = value;
            }
        }

        protected bool CanRebind {
            get { return canRebind; }
        }

        protected TextView DateTextView { get; private set; }

        protected TextView DurationTextView { get; private set; }

        protected EditText StartTimeEditText { get; private set; }

        protected EditText StopTimeEditText { get; private set; }

        protected EditText DescriptionEditText { get; private set; }

        protected EditText ProjectEditText { get; private set; }

        protected EditText TagsEditText { get; private set; }

        protected CheckBox BillableCheckBox { get; private set; }

        protected ImageButton DeleteImageButton { get; private set; }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            if (TimeEntryId != Guid.Empty) {
                TimeEntry = Model.ById<TimeEntryModel> (TimeEntryId);
            }
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle state)
        {
            var view = inflater.Inflate (Resource.Layout.EditTimeEntryFragment, container, false);

            DateTextView = view.FindViewById<TextView> (Resource.Id.DateTextView).SetFont (Font.Roboto);
            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont (Font.Roboto);
            StartTimeEditText = view.FindViewById<EditText> (Resource.Id.StartTimeEditText).SetFont (Font.Roboto);
            StopTimeEditText = view.FindViewById<EditText> (Resource.Id.StopTimeEditText).SetFont (Font.Roboto);
            DescriptionEditText = view.FindViewById<EditText> (Resource.Id.DescriptionEditText).SetFont (Font.RobotoLight);
            ProjectEditText = view.FindViewById<EditText> (Resource.Id.ProjectEditText).SetFont (Font.RobotoLight);
            TagsEditText = view.FindViewById<EditText> (Resource.Id.TagsEditText).SetFont (Font.RobotoLight);
            BillableCheckBox = view.FindViewById<CheckBox> (Resource.Id.BillableCheckBox).SetFont (Font.RobotoLight);
            DeleteImageButton = view.FindViewById<ImageButton> (Resource.Id.DeleteImageButton);

            DurationTextView.Click += OnDurationTextViewClick;
            StartTimeEditText.Click += OnStartTimeEditTextClick;
            StopTimeEditText.Click += OnStopTimeEditTextClick;
            DescriptionEditText.TextChanged += OnDescriptionTextChanged;
            DescriptionEditText.EditorAction += OnDescriptionEditorAction;
            DescriptionEditText.FocusChange += OnDescriptionFocusChange;
            ProjectEditText.Click += OnProjectEditTextClick;
            TagsEditText.Click += OnTagsEditTextClick;
            BillableCheckBox.CheckedChange += OnBillableCheckBoxCheckedChange;
            DeleteImageButton.Click += OnDeleteImageButtonClick;

            return view;
        }

        private void OnDurationTextViewClick (object sender, EventArgs e)
        {
            if (model == null)
                return;
            new ChangeTimeEntryDurationDialogFragment (model).Show (FragmentManager, "duration_dialog");
        }

        private void OnStartTimeEditTextClick (object sender, EventArgs e)
        {
            if (model == null)
                return;
            new ChangeTimeEntryStartTimeDialogFragment (model).Show (FragmentManager, "time_dialog");
        }

        private void OnStopTimeEditTextClick (object sender, EventArgs e)
        {
            if (model == null || model.State == TimeEntryState.Running)
                return;
            new ChangeTimeEntryStopTimeDialogFragment (model).Show (FragmentManager, "time_dialog");
        }

        private void OnDescriptionTextChanged (object sender, Android.Text.TextChangedEventArgs e)
        {
            // Mark description as changed
            descriptionChanging = TimeEntry != null && DescriptionEditText.Text != TimeEntry.Description;

            // Make sure that we're commiting 1 second after the user has stopped typing
            CancelDescriptionChangeAutoCommit ();
            ScheduleDescriptionChangeAutoCommit ();
        }

        private void OnDescriptionFocusChange (object sender, View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus)
                CommitDescriptionChanges ();
        }

        private void OnDescriptionEditorAction (object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done) {
                CommitDescriptionChanges ();
            }
            e.Handled = false;
        }

        private void OnProjectEditTextClick (object sender, EventArgs e)
        {
            if (TimeEntry == null)
                return;

            new ChooseTimeEntryProjectDialogFragment (TimeEntry).Show (FragmentManager, "projects_dialog");
        }

        private void OnTagsEditTextClick (object sender, EventArgs e)
        {
            if (TimeEntry == null)
                return;

            new ChooseTimeEntryTagsDialogFragment (TimeEntry).Show (FragmentManager, "tags_dialog");
        }

        private void OnBillableCheckBoxCheckedChange (object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (TimeEntry == null)
                return;

            TimeEntry.IsBillable = !BillableCheckBox.Checked;
        }

        private void OnDeleteImageButtonClick (object sender, EventArgs e)
        {
            if (TimeEntry == null)
                return;

            TimeEntry.Delete ();
            TimeEntry = TimeEntryModel.GetDraft ();

            Toast.MakeText (Activity, Resource.String.CurrentTimeEntryEditDeleteToast, ToastLength.Short).Show ();
        }

        private void AutoCommitDescriptionChanges ()
        {
            if (!autoCommitScheduled)
                return;
            autoCommitScheduled = false;
            CommitDescriptionChanges ();
        }

        private void ScheduleDescriptionChangeAutoCommit ()
        {
            if (autoCommitScheduled)
                return;

            autoCommitScheduled = true;
            handler.PostDelayed (AutoCommitDescriptionChanges, 1000);
        }

        private void CancelDescriptionChangeAutoCommit ()
        {
            if (!autoCommitScheduled)
                return;

            handler.RemoveCallbacks (AutoCommitDescriptionChanges);
            autoCommitScheduled = false;
        }

        private void CommitDescriptionChanges ()
        {
            if (TimeEntry != null && descriptionChanging) {
                TimeEntry.Description = DescriptionEditText.Text;
            }
            descriptionChanging = false;
            CancelDescriptionChangeAutoCommit ();
        }

        private void DiscardDescriptionChanges ()
        {
            descriptionChanging = false;
            CancelDescriptionChangeAutoCommit ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            canRebind = true;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);

            Rebind ();
        }

        public override void OnStop ()
        {
            base.OnStop ();

            canRebind = false;

            if (subscriptionModelChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }
        }

        public override bool UserVisibleHint {
            get { return base.UserVisibleHint; }
            set {
                if (!value) {
                    CommitDescriptionChanges ();
                }
                base.UserVisibleHint = value;
            }
        }

        protected virtual void OnModelChanged (ModelChangedMessage msg)
        {
            if (msg.Model == TimeEntry) {
                // Listen for changes regarding current running entry
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyStartTime
                    || msg.PropertyName == TimeEntryModel.PropertyStopTime
                    || msg.PropertyName == TimeEntryModel.PropertyDescription
                    || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                    || msg.PropertyName == TimeEntryModel.PropertyProjectId
                    || msg.PropertyName == TimeEntryModel.PropertyTaskId
                    || msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                    Rebind ();
                }
            } else if (TimeEntry != null && TimeEntry.ProjectId == msg.Model.Id && TimeEntry.Project == msg.Model) {
                if (msg.PropertyName == ProjectModel.PropertyName
                    || msg.PropertyName == ProjectModel.PropertyColor
                    || msg.PropertyName == ProjectModel.PropertyClientId) {
                    Rebind ();
                }
            } else if (TimeEntry != null && TimeEntry.TaskId == msg.Model.Id && TimeEntry.Task == msg.Model) {
                if (msg.PropertyName == TaskModel.PropertyName) {
                    Rebind ();
                }
            } else if (TimeEntry != null && TimeEntry.ProjectId != null
                       && model.Project.ClientId == msg.Model.Id && TimeEntry.Project.Client == msg.Model) {
                if (msg.PropertyName == ClientModel.PropertyName) {
                    Rebind ();
                }
            } else if (TimeEntry != null && msg.Model is TimeEntryTagModel) {
                var inter = (TimeEntryTagModel)msg.Model;
                if (inter.FromId == TimeEntry.Id) {
                    // Schedule rebind, as if we do it right away the RelatedModelsCollection will not
                    // have been updated yet
                    Android.App.Application.SynchronizationContext.Post ((state) => {
                        Rebind ();
                    }, null);
                }
            }
        }

        protected virtual void Rebind ()
        {
            if (TimeEntry == null || !canRebind)
                return;

            var res = Resources;

            var startTime = TimeEntry.StartTime;
            if (TimeEntry.StartTime == DateTime.MinValue) {
                startTime = DateTime.Now;

                DurationTextView.Text = TimeSpan.Zero.ToString ();

                // Make sure that we display accurate time:
                handler.RemoveCallbacks (Rebind);
                handler.PostDelayed (Rebind, 5000);
            } else {
                startTime = TimeEntry.StartTime.ToLocalTime ();

                var duration = TimeEntry.GetDuration ();
                DurationTextView.Text = TimeSpan.FromSeconds ((long)duration.TotalSeconds).ToString ();

                if (TimeEntry.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (Rebind);
                    handler.PostDelayed (Rebind, 1000 - duration.Milliseconds);
                }
            }

            StartTimeEditText.Text = startTime.ToDeviceTimeString ();
            if (startTime.Date != DateTime.Now.Date) {
                DateTextView.Text = startTime.ToDeviceDateString ();
                DateTextView.Visibility = ViewStates.Visible;
            } else {
                DateTextView.Visibility = ViewStates.Invisible;
            }

            // Only update DescriptionEditText when content differs, else the user is unable to edit it
            if (!descriptionChanging && DescriptionEditText.Text != TimeEntry.Description) {
                DescriptionEditText.Text = TimeEntry.Description;
            }

            if (TimeEntry.StopTime.HasValue) {
                StopTimeEditText.Text = TimeEntry.StopTime.Value.ToLocalTime ().ToDeviceTimeString ();
                StopTimeEditText.Visibility = ViewStates.Visible;
            } else {
                StopTimeEditText.Text = DateTime.Now.ToDeviceTimeString ();
                if (TimeEntry.StartTime == DateTime.MinValue || TimeEntry.State == TimeEntryState.Running) {
                    StopTimeEditText.Visibility = ViewStates.Invisible;
                } else {
                    StopTimeEditText.Visibility = ViewStates.Visible;
                }
            }

            ProjectEditText.Text = TimeEntry.Project != null ? TimeEntry.Project.Name : String.Empty;
            TagsEditText.Text = String.Join (", ", TimeEntry.Tags.Select ((t) => t.To.Name));
            BillableCheckBox.Checked = !TimeEntry.IsBillable;
            if (TimeEntry.IsBillable) {
                BillableCheckBox.SetText (Resource.String.CurrentTimeEntryEditBillableChecked);
            } else {
                BillableCheckBox.SetText (Resource.String.CurrentTimeEntryEditBillableUnchecked);
            }
            if (TimeEntry.Workspace == null || !TimeEntry.Workspace.IsPremium) {
                BillableCheckBox.Visibility = ViewStates.Gone;
            } else {
                BillableCheckBox.Visibility = ViewStates.Visible;
            }
        }

        protected static bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }
    }
}
