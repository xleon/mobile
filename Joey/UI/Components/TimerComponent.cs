using System;
using System.Collections.Generic;
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
    public class TimerComponent
    {
        private readonly Handler handler = new Handler ();
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private TimeEntryModel runningEntry;
        private bool canRebind;

        protected TextView DurationTextView { get; private set; }

        protected Button StopTrackingButton { get; private set; }

        protected Button StartTrackingButton { get; private set; }

        protected View StoppedTimerSection { get; private set; }

        protected View RunningTimerSection { get; private set; }

        public View Root { get; private set; }

        private BaseActivity activity;

        private void FindViews ()
        {
            RunningTimerSection = Root.FindViewById<View> (Resource.Id.RunningTimerSection);
            StoppedTimerSection = Root.FindViewById<View> (Resource.Id.StoppedTimerSection);
            StartTrackingButton = Root.FindViewById<Button> (Resource.Id.StartTrackingButton);
            StopTrackingButton = Root.FindViewById<Button> (Resource.Id.StopTrackingButton);
            DurationTextView = Root.FindViewById<TextView> (Resource.Id.DurationTextView);

            StopTrackingButton.Click += OnStopTrackingButtonClicked;
            StartTrackingButton.Click += OnStartTrackingButtonClicked;
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
            runningEntry = TimeEntryModel.FindRunning ();

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
            if (msg.Model != runningEntry) {
                // When some other time entry becomes IsRunning we need to switch over to that
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                    var entry = msg.Model as TimeEntryModel;
                    if (entry != null && entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                        runningEntry = entry;
                        Rebind ();
                    }
                }
            } else {
                // Listen for changes regarding current running entry
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyStartTime) {
                    if (runningEntry.State != TimeEntryState.Running)
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
            StoppedTimerSection.Visibility = ViewStates.Gone;
            RunningTimerSection.Visibility = ViewStates.Visible;
        }

        void OnDurationTextClicked (object sender, EventArgs e)
        {
            if (runningEntry == null)
                return;
            new ChangeTimeEntryDurationDialogFragment (runningEntry).Show (activity.FragmentManager, "duration_dialog");
        }

        private void ShowStoppedState ()
        {
            RunningTimerSection.Visibility = ViewStates.Gone;
            StoppedTimerSection.Visibility = ViewStates.Visible;
        }

        private void Rebind ()
        {
            if (!canRebind)
                return;

            if (runningEntry != null) {
                ShowRunningState ();

                var duration = runningEntry.GetDuration ();
                DurationTextView.Text = TimeSpan.FromSeconds ((long)duration.TotalSeconds).ToString ();

                // Schedule next rebind:
                handler.PostDelayed (Rebind, 1000 - duration.Milliseconds);
            } else {
                ShowStoppedState ();
            }
        }

        private void OnStopTrackingButtonClicked (object sender, EventArgs e)
        {
            if (runningEntry != null)
                runningEntry.Stop ();
        }

        private void OnStartTrackingButtonClicked (object sender, EventArgs e)
        {
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            var hasProjects = user.GetAvailableProjects ().Any ();

            if (hasProjects) {
                var entry = TimeEntryModel.StartNew ();
                var intent = new Intent (activity, typeof(ChooseProjectActivity));
                intent.PutExtra (ChooseProjectActivity.TimeEntryIdExtra, entry.Id.ToString ());
                activity.StartActivity (intent);
            } else {
                TimeEntryModel.StartNew ();
            }
        }
    }
}
