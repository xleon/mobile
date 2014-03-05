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
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Components
{
    public class TimerComponent
    {
        private static readonly string LogTag = "TimerComponent";
        private readonly Handler handler = new Handler ();
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private TimeEntryModel currentEntry;
        private bool canRebind;
        private bool hideDuration;

        protected TextView DurationTextView { get; private set; }

        protected Button ActionButton { get; private set; }

        public View Root { get; private set; }

        private BaseActivity activity;

        private void FindViews ()
        {
            ActionButton = Root.FindViewById<Button> (Resource.Id.ActionButton);
            DurationTextView = Root.FindViewById<TextView> (Resource.Id.DurationTextView);

            ActionButton.Click += OnActionButtonClicked;
            DurationTextView.Click += OnDurationTextClicked;
        }

        public void OnCreate (BaseActivity activity)
        {
            this.activity = activity;

            Root = LayoutInflater.From (activity).Inflate (Resource.Layout.TimerComponent, null);

            FindViews ();
        }

        public void OnStart ()
        {
            currentEntry = TimeEntryModel.FindRunning () ?? TimeEntryModel.GetDraft ();

            // Start listening for changes model changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);

            canRebind = true;
            Rebind ();
        }

        public void OnStop ()
        {
            canRebind = false;

            // Stop listening for changes model changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Unsubscribe (subscriptionModelChanged);
            subscriptionModelChanged = null;
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (msg.Model == currentEntry) {
                // Listen for changes regarding current running entry
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyStartTime
                    || msg.PropertyName == TimeEntryModel.PropertyStopTime
                    || msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                    if (currentEntry.State == TimeEntryState.Finished || currentEntry.DeletedAt.HasValue) {
                        currentEntry = TimeEntryModel.GetDraft ();
                    }
                    Rebind ();
                }
            } else if (msg.Model is TimeEntryModel) {
                // When some other time entry becomes Running we need to switch over to that
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                    var entry = (TimeEntryModel)msg.Model;
                    if (entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                        currentEntry = entry;
                        Rebind ();
                    }
                }
            }
        }

        private static bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }

        void OnDurationTextClicked (object sender, EventArgs e)
        {
            if (currentEntry == null)
                return;
            new ChangeTimeEntryDurationDialogFragment (currentEntry).Show (activity.FragmentManager, "duration_dialog");
        }

        private void Rebind ()
        {
            if (!canRebind || currentEntry == null)
                return;

            var res = activity.Resources;
            if (currentEntry.State == TimeEntryState.New && currentEntry.StopTime.HasValue) {
                // Save button
                ActionButton.Text = res.GetString (Resource.String.TimerSaveButtonText);
                ActionButton.SetBackgroundColor (res.GetColor (Resource.Color.gray));
            } else if (currentEntry.State == TimeEntryState.Running) {
                // Stop button
                ActionButton.Text = res.GetString (Resource.String.TimerStopButtonText);
                ActionButton.SetBackgroundColor (res.GetColor (Resource.Color.bright_red));
            } else {
                // Start button
                ActionButton.Text = res.GetString (Resource.String.TimerStartButtonText);
                ActionButton.SetBackgroundColor (res.GetColor (Resource.Color.bright_green));
            }

            if (currentEntry.State == TimeEntryState.Running && !HideDuration) {
                var duration = currentEntry.GetDuration ();
                DurationTextView.Text = TimeSpan.FromSeconds ((long)duration.TotalSeconds).ToString ();
                DurationTextView.Visibility = ViewStates.Visible;

                // Schedule next rebind:
                handler.RemoveCallbacks (Rebind);
                handler.PostDelayed (Rebind, 1000 - duration.Milliseconds);
            } else {
                DurationTextView.Visibility = ViewStates.Gone;
            }
        }

        public bool HideDuration {
            get { return hideDuration; }
            set {
                if (hideDuration != value) {
                    hideDuration = value;
                    Rebind ();
                }
            }
        }

        private void OnActionButtonClicked (object sender, EventArgs e)
        {
            if (currentEntry == null)
                return;

            var startedEntry = false;

            try {
                if (currentEntry.State == TimeEntryState.New && currentEntry.StopTime.HasValue) {
                    currentEntry.Store ();
                } else if (currentEntry.State == TimeEntryState.Running) {
                    currentEntry.Stop ();
                } else {
                    currentEntry.Start ();
                    startedEntry = true;
                }
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Warning (LogTag, ex, "Failed to change time entry state.");
            }

            if (startedEntry && currentEntry.Project == null) {
                var user = ServiceContainer.Resolve<AuthManager> ().User;
                var hasProjects = user.GetAvailableProjects ().Any ();

                if (hasProjects) {
                    new ChooseTimeEntryProjectDialogFragment (currentEntry).Show (activity.FragmentManager, "projects_dialog");
                }

                // TODO: Notify outside world that this component started the timer (thus the pager could act)
            }
        }
    }
}
