using System;
using System.Collections.Generic;
using Cirrious.FluentLayouts.Touch;
using CoreGraphics;
using Foundation;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using Toggl.Ross.Data;
using Toggl.Ross.Theme;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public sealed class LeftViewController : UIViewController
    {
        private TogglWindow window;
        private UIButton logButton;
        private UIButton reportsButton;
        private UIButton settingsButton;
        private UIButton feedbackButton;
        private UIButton signOutButton;
        private UIButton[] menuButtons;
        private UILabel usernameLabel;
        private UILabel syncStatusLabel;
        private UIImageView userAvatarImage;
        private UIImageView separatorLineImage;
        private const int horizMargin = 15;


        private UIPanGestureRecognizer _panGesture;
        private CGPoint draggingPoint;
        private const int menuOffset = 60;
        private const int velocityTreshold = 100;

        private Subscription<SyncStartedMessage> drawerSyncStarted;
        private Subscription<SyncFinishedMessage> drawerSyncFinished;
        private long lastSyncInMillis;
        private int syncStatus;
        private const int syncing = 0;
        private const int syncSuccessful = 1;
        private const int syncHadErrors = 2;
        private const int syncFatalError = 3;

        public override void LoadView ()
        {
            base.LoadView ();
            window = AppDelegate.TogglWindow;
            View.BackgroundColor = UIColor.White;

            _panGesture = new UIPanGestureRecognizer (OnPanGesture) {
                CancelsTouchesInView = true
            };

            menuButtons = new[] {
                (logButton = new UIButton ()),
                (reportsButton = new UIButton ()),
                (settingsButton = new UIButton ()),
                (feedbackButton = new UIButton ()),
                (signOutButton = new UIButton ()),
            };
            logButton.SetTitle ("LeftPanelMenuLog".Tr (), UIControlState.Normal);
            logButton.SetImage (UIImage.FromFile ("iconTimer.png"), UIControlState.Normal);

            reportsButton.SetTitle ("LeftPanelMenuReports".Tr (), UIControlState.Normal);
            reportsButton.SetImage (UIImage.FromFile ("iconReports.png"), UIControlState.Normal);

            settingsButton.SetTitle ("LeftPanelMenuSettings".Tr (), UIControlState.Normal);
            settingsButton.SetImage (UIImage.FromFile ("iconSettings.png"), UIControlState.Normal);

            feedbackButton.SetTitle ("LeftPanelMenuFeedback".Tr (), UIControlState.Normal);
            feedbackButton.SetImage (UIImage.FromFile ("iconFeedback.png"), UIControlState.Normal);

            signOutButton.SetTitle ("LeftPanelMenuSignOut".Tr (), UIControlState.Normal);
            signOutButton.SetImage (UIImage.FromFile ("iconLogout.png"), UIControlState.Normal);


            logButton.HorizontalAlignment = reportsButton.HorizontalAlignment = settingsButton.HorizontalAlignment =
                                                feedbackButton.HorizontalAlignment = signOutButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;

            var authManager = ServiceContainer.Resolve<AuthManager> ();
            authManager.PropertyChanged += OnUserLoad;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            drawerSyncStarted = bus.Subscribe<SyncStartedMessage> (SyncStarted);
            drawerSyncFinished = bus.Subscribe<SyncFinishedMessage> (SyncFinished);

            foreach (var button in menuButtons) {
                button.Apply (Style.LeftView.Button);
                button.TouchUpInside += OnMenuButtonTouchUpInside;
                View.AddSubview (button);
            }

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            View.AddConstraints (MakeConstraints (View));
            View.AddGestureRecognizer (_panGesture);
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            usernameLabel = new UILabel ().Apply (Style.LeftView.UserLabel);
            usernameLabel.Frame = new CGRect (60, View.Frame.Height - 100f, height: 50f, width: View.Frame.Width);
            View.AddSubview (usernameLabel);

            userAvatarImage = new UIImageView (
                new CGRect (
                    View.Frame.Width - menuOffset - 90f,
                    View.Frame.Height - 100f,
                    60f,
                    60f
                ));
            userAvatarImage.Layer.CornerRadius = 30f;
            userAvatarImage.Layer.MasksToBounds = true;
            View.AddSubview (userAvatarImage);

            separatorLineImage = new UIImageView (UIImage.FromFile ("line.png"));
            separatorLineImage.Frame = new CGRect (0f, View.Frame.Height - 140f, height: 1f, width: View.Frame.Width - menuOffset);
            View.AddSubview (separatorLineImage);

            syncStatusLabel = new UILabel ().Apply (Style.LeftView.UserLabel);
            syncStatusLabel.Frame = new CGRect (horizMargin, View.Frame.Height - 50f, height: 50f, width: View.Frame.Width);
            View.AddSubview (syncStatusLabel);
        }

        private void OnUserLoad (object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var userData = ServiceContainer.Resolve<AuthManager> ().User;
            usernameLabel.Text = userData.Name;

            var url = new NSUrl (userData.ImageUrl);
            var data = NSData.FromUrl (url);
            var image = UIImage.LoadFromData (data);
            userAvatarImage.Image = image;
        }

        public nfloat MaxDraggingX
        {
            get {
                return View.Frame.Width - menuOffset;
            }
        }

        public nfloat MinDraggingX
        {
            get {
                return 0;
            }
        }

        private void OnPanGesture (UIPanGestureRecognizer recognizer)
        {
            var translation = recognizer.TranslationInView (recognizer.View);
            var movement = translation.X - draggingPoint.X;
            var main = window.RootViewController as MainViewController;
            var currentX = main.View.Frame.X;

            switch (recognizer.State) {
            case UIGestureRecognizerState.Began:
                draggingPoint = translation;
                break;

            case UIGestureRecognizerState.Changed:
                var newX = currentX;
                newX += movement;
                if (newX > MinDraggingX && newX < MaxDraggingX) {
                    main.MoveToLocation (newX);
                }
                draggingPoint = translation;
                break;

            case UIGestureRecognizerState.Ended:
                if (Math.Abs (translation.X) >= velocityTreshold) {
                    if (translation.X < 0) {
                        main.CloseMenu ();
                    } else {
                        main.OpenMenu ();
                    }
                } else {
                    if (Math.Abs (currentX) < (View.Frame.Width - menuOffset) / 2) {
                        main.CloseMenu ();
                    } else {
                        main.OpenMenu ();
                    }
                }
                break;
            }
        }

        private static IEnumerable<FluentLayout> MakeConstraints (UIView container)
        {
            UIView prev = null;
            const float startTopMargin = 60.0f;
            const float topMargin = 7f;

            foreach (var view in container.Subviews) {
                if (! (view is UIButton)) {
                    continue;
                }

                if (prev == null) {
                    yield return view.AtTopOf (container, topMargin + startTopMargin);
                } else {
                    yield return view.Below (prev, topMargin);
                }

                yield return view.AtLeftOf (container, horizMargin);
                yield return view.AtRightOf (container, horizMargin + 20);

                prev = view;
            }
        }

        private void OnMenuButtonTouchUpInside (object sender, EventArgs e)
        {
            var main = window.RootViewController as MainViewController;
            if (sender == logButton) {
                ServiceContainer.Resolve<SettingsStore> ().PreferredStartView = "log";
                main.SetViewControllers (new[] {
                    new LogViewController ()
                }, true);
            } else if (sender == reportsButton) {
                main.PushViewController (new ReportsViewController (), true);
            } else if (sender == settingsButton) {
                main.PushViewController (new SettingsViewController (), true);
            } else if (sender == feedbackButton) {
                main.PushViewController (new FeedbackViewController (), true);
            } else {
                ServiceContainer.Resolve<AuthManager> ().Forget ();
            }
            main.CloseMenu();
        }

        protected void SyncStarted (SyncStartedMessage msg)
        {
            syncStatus = syncing;
            UpdateSyncStatus();
        }

        private void SyncFinished (SyncFinishedMessage msg)
        {
            if (msg.FatalError != null) {
                syncStatus = syncFatalError;
            } else if (msg.HadErrors) {
                syncStatus = syncHadErrors;
            } else {
                syncStatus = syncSuccessful;
            }
            lastSyncInMillis = Toggl.Phoebe.Time.Now.Ticks / TimeSpan.TicksPerMillisecond;
            UpdateSyncStatus ();
        }

        private void UpdateSyncStatus ()
        {
            switch (syncStatus) {
            case syncing:
                syncStatusLabel.Text = "LeftPanelSyncing".Tr();
                break;
            case syncHadErrors:
                syncStatusLabel.Text = "LeftPanelSyncHadErrors".Tr();
                break;
            case syncFatalError:
                syncStatusLabel.Text = "LeftPanelSyncFailed".Tr();
                break;
            default:
                syncStatusLabel.Text = ResolveLastSyncTime ();
                break;
            }
        }

        private String ResolveLastSyncTime ()
        {
            const int minuteInMillis = 60 * 1000;
            var NowInMillis = Toggl.Phoebe.Time.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (NowInMillis - lastSyncInMillis < minuteInMillis) {
                return "LeftPanelSyncJustNow".Tr();
            }

            return String.Format (
                       "LeftPanelSyncTime".Tr(),
                       (NowInMillis - lastSyncInMillis) / minuteInMillis
                   );
        }
    }
}
