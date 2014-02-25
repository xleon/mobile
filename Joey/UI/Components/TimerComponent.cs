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
        private object subscriptionModelChanged;
        private TimeEntryModel runningEntry;

        protected TextView DurationTextView { get; private set; }
        protected Button StopTrackingButton { get; private set; }
        protected Button StartTrackingButton { get; private set; }
        protected View StoppedTimerSection { get; private set; }
        protected View RunningTimerSection { get; private set; }

        public View Root { get; private set; }
        private Context ctx;

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

        public void OnCreate (Context ctx)
        {
            this.ctx = ctx;

            Root = LayoutInflater.From (ctx).Inflate (Resource.Layout.TimerComponent, null);

            FindViews ();
        }

        public void OnStart ()
        {
            runningEntry = TimeEntryModel.FindRunning();

            // Start listening for changes model changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);

            Rebind ();
        }

        public void OnStop ()
        {
            // Stop listening for changes model changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Unsubscribe (subscriptionModelChanged);
            subscriptionModelChanged = null;
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
            StoppedTimerSection.Visibility = ViewStates.Gone;
            RunningTimerSection.Visibility = ViewStates.Visible;
        }

        void OnDurationTextClicked (object sender, EventArgs e)
        {
            long minutesInHours = 60 * 60;
            int hours = (int) (runningEntry.Duration / minutesInHours);
            int minutes = (int) ((runningEntry.Duration % minutesInHours) / 60);
            var dialog = new TimePickerDialog (ctx, OnDurationSelected, hours, minutes, true);
            dialog.Show ();
        }

        void OnDurationSelected (object sender, TimePickerDialog.TimeSetEventArgs timeSetArgs)
        {
            runningEntry.Duration = timeSetArgs.HourOfDay * 60  * 60 + timeSetArgs.Minute * 60;
            //TODO Next line here just to make it work somehow, magic of changing start/stop time after duration change will be in Model
            runningEntry.StartTime.Subtract (new TimeSpan (timeSetArgs.HourOfDay, timeSetArgs.Minute, 0));
        }

        private void ShowStoppedState ()
        {
            RunningTimerSection.Visibility = ViewStates.Gone;
            StoppedTimerSection.Visibility = ViewStates.Visible;
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
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            var hasProjects = user.GetAvailableProjects ().Any ();

            if (hasProjects) {
                var entry = TimeEntryModel.StartNew ();
                var intent = new Intent (ctx, typeof(ChooseProjectActivity));
                intent.PutExtra (ChooseProjectActivity.TimeEntryIdExtra, entry.Id.ToString ());
                ctx.StartActivity (intent);
            } else {
                TimeEntryModel.StartNew ();
            }
        }
    }
}
