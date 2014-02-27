using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Activities;
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

        private TimeEntryModel Model {
            get { return model; }
            set {
                DiscardDescriptionChanges ();
                model = value;
            }
        }

        protected TextView DurationTextView { get; private set; }

        protected EditText StartTimeEditText { get; private set; }

        protected EditText StopTimeEditText { get; private set; }

        protected EditText DateEditText { get; private set; }

        protected EditText DescriptionEditText { get; private set; }

        protected EditText ProjectEditText { get; private set; }

        protected EditText TagsEditText { get; private set; }

        protected CheckBox BillableCheckBox { get; private set; }

        protected ImageButton DeleteImageButton { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle state)
        {
            var view = inflater.Inflate (Resource.Layout.EditCurrentTimeEntryFragment, container, false);

            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            StartTimeEditText = view.FindViewById<EditText> (Resource.Id.StartTimeEditText);
            StopTimeEditText = view.FindViewById<EditText> (Resource.Id.StopTimeEditText);
            DateEditText = view.FindViewById<EditText> (Resource.Id.DateEditText);
            DescriptionEditText = view.FindViewById<EditText> (Resource.Id.DescriptionEditText);
            ProjectEditText = view.FindViewById<EditText> (Resource.Id.ProjectEditText);
            TagsEditText = view.FindViewById<EditText> (Resource.Id.TagsEditText);
            BillableCheckBox = view.FindViewById<CheckBox> (Resource.Id.BillableCheckBox);
            DeleteImageButton = view.FindViewById<ImageButton> (Resource.Id.DeleteImageButton);

            DurationTextView.Click += OnDurationTextViewClick;
            StartTimeEditText.Click += OnStartTimeEditTextClick;
            StopTimeEditText.Click += OnStopTimeEditTextClick;
            DateEditText.Click += OnDateEditTextClick;
            DescriptionEditText.TextChanged += OnDescriptionTextChanged;
            DescriptionEditText.EditorAction += OnDescriptionEditorAction;
            DescriptionEditText.FocusChange += OnDescriptionFocusChange;
            ProjectEditText.Click += OnProjectEditTextClick;
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

        private void OnDateEditTextClick (object sender, EventArgs e)
        {
            if (model == null)
                return;
            new ChangeTimeEntryDateDialogFragment (model).Show (FragmentManager, "date_dialog");
        }

        private void OnDescriptionTextChanged (object sender, Android.Text.TextChangedEventArgs e)
        {
            // Mark description as changed
            descriptionChanging = Model != null && DescriptionEditText.Text != Model.Description;
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

            var intent = new Intent (Activity, typeof(ChooseProjectActivity));
            intent.PutExtra (ChooseProjectActivity.TimeEntryIdExtra, Model.Id.ToString ());
            Activity.StartActivity (intent);
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

        private void CommitDescriptionChanges ()
        {
            if (Model != null && descriptionChanging) {
                Model.Description = DescriptionEditText.Text;
            }
            descriptionChanging = false;
        }

        private void DiscardDescriptionChanges ()
        {
            descriptionChanging = false;
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
                    Rebind ();
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

            if (Model.StartTime == DateTime.MinValue) {
                var now = DateTime.Now;

                DurationTextView.Text = TimeSpan.Zero.ToString ();
                StartTimeEditText.Text = now.ToShortTimeString ();
                DateEditText.Text = now.ToShortDateString ();

                // Make sure that we display accurate time:
                handler.RemoveCallbacks (Rebind);
                handler.PostDelayed (Rebind, 5000);
            } else {
                var duration = Model.GetDuration ();
                DurationTextView.Text = TimeSpan.FromSeconds ((long)duration.TotalSeconds).ToString ();

                var startTime = Model.StartTime.ToLocalTime ();
                StartTimeEditText.Text = startTime.ToShortTimeString ();
                DateEditText.Text = startTime.ToShortDateString ();

                if (Model.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (Rebind);
                    handler.PostDelayed (Rebind, 1000 - duration.Milliseconds);
                }
            }

            // Only update DescriptionEditText when content differs, else the user is unable to edit it
            if (!descriptionChanging && DescriptionEditText.Text != Model.Description) {
                DescriptionEditText.Text = Model.Description;
            }

            if (Model.StopTime.HasValue) {
                StopTimeEditText.Text = Model.StopTime.Value.ToLocalTime ().ToShortTimeString ();
            } else {
                StopTimeEditText.Text = DateTime.Now.ToShortTimeString ();
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
