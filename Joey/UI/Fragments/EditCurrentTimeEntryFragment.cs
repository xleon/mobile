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
    public class EditCurrentTimeEntryFragment : Fragment
    {
        private readonly Handler handler = new Handler ();
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private TimeEntryModel model;
        private bool canRebind;
        private bool descriptionChanging;
        private bool autoCommitScheduled;

        private TimeEntryModel Model {
            get { return model; }
            set {
                DiscardDescriptionChanges ();
                model = value;
            }
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

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle state)
        {
            var view = inflater.Inflate (Resource.Layout.EditCurrentTimeEntryFragment, container, false);

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
            descriptionChanging = Model != null && DescriptionEditText.Text != Model.Description;

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
            if (Model == null)
                return;

            new ChooseTimeEntryProjectDialogFragment (Model).Show (FragmentManager, "projects_dialog");
        }

        private void OnTagsEditTextClick (object sender, EventArgs e)
        {
            if (Model == null)
                return;

            new ChooseTimeEntryTagsDialogFragment (Model).Show (FragmentManager, "tags_dialog");
        }

        private void OnBillableCheckBoxCheckedChange (object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (Model == null)
                return;

            Model.IsBillable = !BillableCheckBox.Checked;
        }

        private void OnDeleteImageButtonClick (object sender, EventArgs e)
        {
            if (Model == null)
                return;

            Model.Delete ();
            Model = TimeEntryModel.GetDraft ();

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
            if (Model != null && descriptionChanging) {
                Model.Description = DescriptionEditText.Text;
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

            Model = TimeEntryModel.FindRunning () ?? TimeEntryModel.GetDraft ();
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

        protected void OnModelChanged (ModelChangedMessage msg)
        {
            if (msg.Model == Model) {
                // Listen for changes regarding current running entry
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyStartTime
                    || msg.PropertyName == TimeEntryModel.PropertyStopTime
                    || msg.PropertyName == TimeEntryModel.PropertyDescription
                    || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                    || msg.PropertyName == TimeEntryModel.PropertyProjectId
                    || msg.PropertyName == TimeEntryModel.PropertyTaskId
                    || msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                    if (Model.State == TimeEntryState.Finished || Model.DeletedAt.HasValue) {
                        Model = TimeEntryModel.GetDraft ();
                    }
                    Rebind ();
                }
            } else if (Model != null && Model.ProjectId == msg.Model.Id && Model.Project == msg.Model) {
                if (msg.PropertyName == ProjectModel.PropertyName
                    || msg.PropertyName == ProjectModel.PropertyColor
                    || msg.PropertyName == ProjectModel.PropertyClientId) {
                    Rebind ();
                }
            } else if (Model != null && Model.TaskId == msg.Model.Id && Model.Task == msg.Model) {
                if (msg.PropertyName == TaskModel.PropertyName) {
                    Rebind ();
                }
            } else if (Model != null && Model.ProjectId != null
                       && model.Project.ClientId == msg.Model.Id && Model.Project.Client == msg.Model) {
                if (msg.PropertyName == ClientModel.PropertyName) {
                    Rebind ();
                }
            } else if (Model != null && msg.Model is TimeEntryTagModel) {
                var inter = (TimeEntryTagModel)msg.Model;
                if (inter.FromId == Model.Id) {
                    // Schedule rebind, as if we do it right away the RelatedModelsCollection will not
                    // have been updated yet
                    Android.App.Application.SynchronizationContext.Post ((state) => {
                        Rebind ();
                    }, null);
                }
            } else if (msg.Model is TimeEntryModel) {
                // When some other time entry becomes IsRunning we need to switch over to that
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                    var entry = (TimeEntryModel)msg.Model;
                    if (entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                        Model = entry;
                        Rebind ();
                    }
                }
            }
        }

        protected void Rebind ()
        {
            if (Model == null || !canRebind)
                return;

            var res = Resources;

            var startTime = Model.StartTime;
            if (Model.StartTime == DateTime.MinValue) {
                startTime = DateTime.Now;

                DurationTextView.Text = TimeSpan.Zero.ToString ();

                // Make sure that we display accurate time:
                handler.RemoveCallbacks (Rebind);
                handler.PostDelayed (Rebind, 5000);
            } else {
                startTime = Model.StartTime.ToLocalTime ();

                var duration = Model.GetDuration ();
                DurationTextView.Text = TimeSpan.FromSeconds ((long)duration.TotalSeconds).ToString ();

                if (Model.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (Rebind);
                    handler.PostDelayed (Rebind, 1000 - duration.Milliseconds);
                }
            }

            StartTimeEditText.Text = startTime.ToShortTimeString ();
            if (startTime.Date != DateTime.Now.Date) {
                DateTextView.Text = startTime.ToShortDateString ();
                DateTextView.Visibility = ViewStates.Visible;
            } else {
                DateTextView.Visibility = ViewStates.Invisible;
            }

            // Only update DescriptionEditText when content differs, else the user is unable to edit it
            if (!descriptionChanging && DescriptionEditText.Text != Model.Description) {
                DescriptionEditText.Text = Model.Description;
            }

            if (Model.StopTime.HasValue) {
                StopTimeEditText.Text = Model.StopTime.Value.ToLocalTime ().ToShortTimeString ();
                StopTimeEditText.Visibility = ViewStates.Visible;
            } else {
                StopTimeEditText.Text = DateTime.Now.ToShortTimeString ();
                if (Model.StartTime == DateTime.MinValue || Model.State == TimeEntryState.Running) {
                    StopTimeEditText.Visibility = ViewStates.Invisible;
                } else {
                    StopTimeEditText.Visibility = ViewStates.Visible;
                }
            }

            ProjectEditText.Text = Model.Project != null ? Model.Project.Name : String.Empty;
            TagsEditText.Text = String.Join (", ", Model.Tags.Select ((t) => t.To.Name));
            BillableCheckBox.Checked = !Model.IsBillable;
            if (Model.IsBillable) {
                BillableCheckBox.SetText (Resource.String.CurrentTimeEntryEditBillableChecked);
            } else {
                BillableCheckBox.SetText (Resource.String.CurrentTimeEntryEditBillableUnchecked);
            }
        }

        private static bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }
    }
}
