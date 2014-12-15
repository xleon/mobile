using System;
using System.Drawing;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Data;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public abstract class SyncStatusViewController : UIViewController
    {
        private const string RotateAnimationKey = "rotateAnim";
        private const int StatusBarHeight = 60;

        private readonly UIViewController contentViewController;
        private Subscription<SyncStartedMessage> subscriptionSyncStarted;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private StatusView statusView;
        private bool showStatus;

        protected SyncStatusViewController (UIViewController viewController)
        {
            if (viewController == null) {
                throw new ArgumentNullException ("viewController");
            }

            contentViewController = viewController;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSyncStarted = bus.Subscribe<SyncStartedMessage> (OnSyncStarted);
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
        }

        private void OnSyncStarted (SyncStartedMessage msg)
        {
            if (StatusBarShown) {
                statusView.IsSyncing = true;
            }
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            if (msg.HadErrors) {
                var ignoreTime = settingsStore.IgnoreSyncErrorsUntil;
                var showError = ignoreTime == null || ignoreTime < Time.UtcNow;

                if (showError) {
                    // Make sure that error is shown
                    statusView.IsSyncing = false;
                    StatusBarShown = true;
                }
            } else {
                // Successful sync, clear ignoring flag
                settingsStore.IgnoreSyncErrorsUntil = null;
                StatusBarShown = false;
            }
        }

        private bool StatusBarShown
        {
            get { return showStatus; }
            set {
                if (showStatus == value) {
                    return;
                }
                showStatus = value;
                UIView.Animate (0.5f, LayoutStatusBar);
            }
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionSyncStarted != null) {
                bus.Unsubscribe (subscriptionSyncStarted);
                subscriptionSyncStarted = null;
            }
            if (subscriptionSyncFinished != null) {
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        public override void LoadView ()
        {
            var view = new UIView ();

            view.Add (statusView = new StatusView () {
                Retry = RetrySync,
                Cancel = DismissMessage,
            });

            View = view;
        }

        private void RetrySync ()
        {
            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            syncManager.Run ();
        }

        private void DismissMessage ()
        {
            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            settingsStore.IgnoreSyncErrorsUntil = Time.UtcNow + TimeSpan.FromMinutes (5);
            StatusBarShown = false;
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            // Add content view controller:
            AddChildViewController (contentViewController);
            contentViewController.View.Frame = new RectangleF (PointF.Empty, View.Frame.Size);
            View.InsertSubviewBelow (contentViewController.View, statusView);
            contentViewController.DidMoveToParentViewController (this);
        }

        public override void ViewDidLayoutSubviews ()
        {
            base.ViewDidLayoutSubviews ();

            LayoutStatusBar ();
        }

        private void LayoutStatusBar ()
        {
            var size = View.Frame.Size;

            var statusY = showStatus ? size.Height - StatusBarHeight : size.Height + 2f;
            statusView.Frame = new RectangleF (
                0, statusY, size.Width, StatusBarHeight);
        }

        public override UINavigationItem NavigationItem
        {
            get { return contentViewController.NavigationItem; }
        }

        public class StatusView : UIView
        {
            private UIButton retryButton;
            private UIButton cancelButton;
            private UILabel statusLabel;
            private bool isSyncing;
            private string statusSyncingText;
            private string statusFailText;

            public StatusView ()
            {
                Add (retryButton = new UIButton ().Apply (Style.SyncStatus.RetryButton));
                Add (cancelButton = new UIButton ().Apply (Style.SyncStatus.CancelButton));
                Add (statusLabel = new UILabel ().Apply (Style.SyncStatus.StatusLabel));

                retryButton.TouchUpInside += (s, e) => {
                    if (Retry != null) {
                        Retry ();
                    }
                };
                cancelButton.TouchUpInside += (s, e) => {
                    if (Cancel != null) {
                        Cancel ();
                    }
                };

                this.Apply (Style.SyncStatus.BarBackground);

                statusSyncingText = "SyncStatusSyncing".Tr ();
                statusFailText = "SyncStatusFail".Tr ();

                ResetState ();
            }

            public Action Retry { get; set; }

            public Action Cancel { get; set; }

            public string StatusFailText
            {
                get {
                    return statusFailText;
                } set {
                    statusFailText = value;
                    ResetState ();
                }
            }

            public string StatusSyncingText
            {
                get {
                    return statusSyncingText;
                } set {
                    statusSyncingText = value;
                    ResetState ();
                }
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var size = Frame.Size;

                var buttonDimension = size.Height;
                retryButton.Frame = new RectangleF (
                    0, (size.Height - buttonDimension) / 2,
                    buttonDimension, buttonDimension);
                cancelButton.Frame = new RectangleF (
                    size.Width - retryButton.Frame.Width,
                    (size.Height - retryButton.Frame.Height) / 2,
                    buttonDimension, buttonDimension);

                var statusX = retryButton.Frame.X + retryButton.Frame.Width;
                statusLabel.Frame = new RectangleF (
                    statusX, 0, cancelButton.Frame.X - statusX, size.Height);
            }

            private void ResetState ()
            {
                if (isSyncing) {
                    retryButton.UserInteractionEnabled = cancelButton.UserInteractionEnabled = false;
                    statusLabel.Text = statusSyncingText;
                    cancelButton.Hidden = true;

                    StartRetryRotation ();
                } else {
                    retryButton.UserInteractionEnabled = cancelButton.UserInteractionEnabled = true;
                    statusLabel.Text = statusFailText;
                    cancelButton.Hidden = false;
                }
            }

            private void StartRetryRotation ()
            {
                var layer = retryButton.ImageView.Layer;

                var anim = layer.AnimationForKey (RotateAnimationKey) as CABasicAnimation;
                if (anim == null) {
                    anim = CABasicAnimation.FromKeyPath ("transform.rotation.z");
                    anim.From = NSNumber.FromFloat (0f);
                    anim.To = NSNumber.FromFloat (2f * (float)Math.PI);
                    anim.Duration = 2f;

                    layer.AddAnimation (anim, RotateAnimationKey);
                }
            }

            public bool IsSyncing
            {
                get { return isSyncing; }
                set {
                    if (isSyncing == value) {
                        return;
                    }
                    isSyncing = value;
                    ResetState ();
                }
            }
        }
    }
}
