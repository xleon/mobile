using System;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class TimerNavigationController
    {
        private const string DefaultDurationText = " 00:00:00 ";
        private UILabel durationLabel;
        private UIButton actionButton;
        private UIBarButtonItem navigationButton;
        private TimeEntryModel currentTimeEntry;
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private bool isStarted;
        private int rebindCounter;

        public void Attach (UINavigationItem navigationItem)
        {
            // Lazyily create views
            if (durationLabel == null) {
                durationLabel = new UILabel () {
                    Text = DefaultDurationText, // Dummy content to use for sizing of the label
                }.ApplyStyle (Style.NavTimer.DurationLabel);
                durationLabel.SizeToFit ();
            }

            if (navigationButton == null) {
                actionButton = new UIButton ().ApplyStyle (Style.NavTimer.StartButton);
                actionButton.SizeToFit ();
                navigationButton = new UIBarButtonItem (actionButton);
            }

            // Attach views
            navigationItem.TitleView = durationLabel;
            navigationItem.RightBarButtonItem = navigationButton;
        }

        private void Rebind ()
        {
            if (!isStarted)
                return;

            rebindCounter++;

            if (currentTimeEntry == null) {
                durationLabel.Text = DefaultDurationText;
                actionButton.ApplyStyle (Style.NavTimer.StartButton);
            } else {
                var duration = currentTimeEntry.GetDuration ();

                durationLabel.Text = duration.ToString (@"hh\:mm\:ss");
                actionButton.ApplyStyle (Style.NavTimer.StopButton);

                var counter = rebindCounter;
                DispatchQueue.MainQueue.DispatchAfter (
                    TimeSpan.FromMilliseconds (1000 - duration.Milliseconds),
                    delegate {
                        if (counter == rebindCounter) {
                            Rebind ();
                        }
                    });
            }
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (msg.Model == currentTimeEntry) {
                // Listen for changes regarding current running entry
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyStartTime
                    || msg.PropertyName == TimeEntryModel.PropertyStopTime
                    || msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                    if (currentTimeEntry.State == TimeEntryState.Finished || currentTimeEntry.DeletedAt.HasValue) {
                        currentTimeEntry = null;
                    }
                    Rebind ();
                }
            } else if (msg.Model is TimeEntryModel) {
                // When some other time entry becomes Running we need to switch over to that
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                    var entry = (TimeEntryModel)msg.Model;
                    if (entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                        currentTimeEntry = entry;
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

        public void Start ()
        {
            // Start listening to timer changes
            currentTimeEntry = TimeEntryModel.FindRunning ();

            if (subscriptionModelChanged == null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            isStarted = true;
            Rebind ();
        }

        public void Stop ()
        {
            // Stop listening to timer changes
            isStarted = false;

            if (subscriptionModelChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }

            currentTimeEntry = null;
        }
    }
}
