using System;
using System.Collections.Generic;
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
        private readonly bool showRunning;
        private UIViewController parentController;
        private UIButton durationButton;
        private UIButton actionButton;
        private UIBarButtonItem navigationButton;
        private TimeEntryModel currentTimeEntry;
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private bool isStarted;
        private int rebindCounter;

        public TimerNavigationController (TimeEntryModel model = null)
        {
            showRunning = model == null;
            currentTimeEntry = model;
        }

        public void Attach (UIViewController parentController)
        {
            this.parentController = parentController;

            // Lazyily create views
            if (durationButton == null) {
                durationButton = new UIButton ().Apply (Style.NavTimer.DurationButton);
                durationButton.SetTitle (DefaultDurationText, UIControlState.Normal); // Dummy content to use for sizing of the label
                durationButton.SizeToFit ();
                durationButton.TouchUpInside += OnDurationButtonTouchUpInside;
            }

            if (navigationButton == null) {
                actionButton = new UIButton ().Apply (Style.NavTimer.StartButton);
                actionButton.SizeToFit ();
                actionButton.TouchUpInside += OnActionButtonTouchUpInside;
                navigationButton = new UIBarButtonItem (actionButton);
            }

            // Attach views
            var navigationItem = parentController.NavigationItem;
            navigationItem.TitleView = durationButton;
            navigationItem.RightBarButtonItem = navigationButton;
        }

        private void OnDurationButtonTouchUpInside (object sender, EventArgs e)
        {
            var controller = new DurationChangeViewController (currentTimeEntry);
            parentController.NavigationController.PushViewController (controller, true);
        }

        private void OnActionButtonTouchUpInside (object sender, EventArgs e)
        {
            if (currentTimeEntry == null) {
                currentTimeEntry = TimeEntryModel.GetDraft ();
                currentTimeEntry.Start ();

                var controllers = new List<UIViewController> (parentController.NavigationController.ViewControllers);
                controllers.Add (new EditTimeEntryViewController (currentTimeEntry));
                controllers.Add (new ProjectSelectionViewController (currentTimeEntry));
                parentController.NavigationController.SetViewControllers (controllers.ToArray (), true);
            } else {
                currentTimeEntry.Stop ();
            }
        }

        private void Rebind ()
        {
            if (!isStarted)
                return;

            rebindCounter++;

            if (currentTimeEntry == null) {
                durationButton.SetTitle (DefaultDurationText, UIControlState.Normal);
                actionButton.Apply (Style.NavTimer.StartButton);
            } else {
                var duration = currentTimeEntry.GetDuration ();

                durationButton.SetTitle (duration.ToString (@"hh\:mm\:ss"), UIControlState.Normal);
                actionButton.Apply (Style.NavTimer.StopButton);
                actionButton.Hidden = currentTimeEntry.State != TimeEntryState.Running;

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
                        if (showRunning) {
                            currentTimeEntry = null;
                        }
                    }
                    Rebind ();
                }
            } else if (showRunning && msg.Model is TimeEntryModel) {
                // When some other time entry becomes Running we need to switch over to that
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared
                    || msg.PropertyName == TimeEntryModel.PropertyIsPersisted) {
                    var entry = (TimeEntryModel)msg.Model;
                    if (entry.IsShared && entry.IsPersisted && entry.DeletedAt == null
                        && entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
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
            if (showRunning) {
                currentTimeEntry = TimeEntryModel.FindRunning ();
            }

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

            if (showRunning) {
                currentTimeEntry = null;
            }
        }
    }
}
