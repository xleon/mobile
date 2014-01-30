using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class TimerFragment : Fragment
    {
        private object subscriptionModelChanged;
        private TimeEntryModel runningEntry;

        protected View RunningStateView { get; private set; }

        protected TextView DurationTextView { get; private set; }

        protected Button StopTrackingButton { get; private set; }

        protected View StoppedStateView { get; private set; }

        protected Button StartTrackingButton { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate (Resource.Layout.TimerFragment, container, false);
        }

        public override void OnStart ()
        {
            base.OnStart ();

            // Load database entries into memeory:
            IEnumerable<TimeEntryModel> entries;
            entries = Model.Query<TimeEntryModel> ((te) => te.IsRunning)
                .NotDeleted ().ForCurrentUser ().ToList ();
            // Find currently running time entry:
            entries = Model.Manager.Cached<TimeEntryModel> ()
                .Where ((te) => te.IsRunning && te.DeletedAt == null)
                .ForCurrentUser ();
            runningEntry = entries.FirstOrDefault ();

            // Start listening for changes model changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);

            Rebind ();
        }

        public override void OnStop ()
        {
            // Stop listening for changes model changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Unsubscribe (subscriptionModelChanged);
            subscriptionModelChanged = null;

            base.OnStop ();
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (msg.Model != runningEntry) {
                // When some other time entry becomes IsRunning we need to switch over to that
                if (msg.PropertyName == TimeEntryModel.PropertyIsRunning
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                    var entry = msg.Model as TimeEntryModel;
                    if (entry != null && entry.IsRunning && ForCurrentUser (entry)) {
                        runningEntry = entry;
                        Rebind ();
                    }
                }
            } else {
                // Listen for changes regarding current running entry
                if (msg.PropertyName == TimeEntryModel.PropertyIsRunning
                    || msg.PropertyName == TimeEntryModel.PropertyDuration) {
                    if (!runningEntry.IsRunning)
                        runningEntry = null;
                    Rebind ();
                }
            }
        }

        private static bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }

        private void ShowRunningState ()
        {
            // Hide other states
            if (StoppedStateView != null) {
                StoppedStateView.Visibility = ViewStates.Gone;
            }

            // Lazy initialise running state
            if (RunningStateView == null) {
                RunningStateView = View.FindViewById<ViewStub> (Resource.Id.TimerRunningViewStub).Inflate ();
                DurationTextView = RunningStateView.FindViewById<TextView> (Resource.Id.DurationTextView);
                StopTrackingButton = RunningStateView.FindViewById<Button> (Resource.Id.StopTrackingButton);

                StopTrackingButton.Click += OnStopTrackingButtonClicked;
            }

            RunningStateView.Visibility = ViewStates.Visible;
        }

        private void ShowStoppedState ()
        {
            // Hide other states
            if (RunningStateView != null) {
                RunningStateView.Visibility = ViewStates.Gone;
            }

            // Lazy initialise stopped state
            if (StoppedStateView == null) {
                StoppedStateView = View.FindViewById<ViewStub> (Resource.Id.TimerStoppedViewStub).Inflate ();
                StartTrackingButton = StoppedStateView.FindViewById<Button> (Resource.Id.StartTrackingButton);

                StartTrackingButton.Click += OnStartTrackingButtonClicked;
            }

            StoppedStateView.Visibility = ViewStates.Visible;
        }

        private void Rebind ()
        {
            if (runningEntry != null) {
                ShowRunningState ();

                DurationTextView.Text = TimeSpan.FromSeconds (runningEntry.Duration).ToString ();
            } else {
                ShowStoppedState ();
            }
        }

        private void OnStopTrackingButtonClicked (object sender, EventArgs e)
        {
            if (runningEntry != null)
                runningEntry.IsRunning = false;
        }

        private void OnStartTrackingButtonClicked (object sender, EventArgs e)
        {
        }
    }
}
