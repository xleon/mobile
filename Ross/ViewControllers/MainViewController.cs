using System;
using Foundation;
using UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Data;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class MainViewController : UINavigationController
    {
        private Subscription<AuthChangedMessage> subscriptionAuthChanged;
        private Subscription<TogglHttpResponseMessage> subscriptionTogglHttpResponse;
        private NavDelegate navDelegate;
        private UIScreenEdgePanGestureRecognizer interactiveEdgePanGestureRecognizer;
        private UIAlertView upgradeAlert;

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.Apply (Style.Screen);
            NavigationBar.Apply (Style.NavigationBar);
            Delegate = navDelegate = new NavDelegate ();

            InteractivePopGestureRecognizer.ShouldBegin = GestureRecognizerShouldBegin;

            interactiveEdgePanGestureRecognizer = new UIScreenEdgePanGestureRecognizer (OnEdgePanGesture) {
                Edges = UIRectEdge.Left,
                ShouldBegin = GestureRecognizerShouldBegin,
            };
            View.AddGestureRecognizer (interactiveEdgePanGestureRecognizer);
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionAuthChanged == null) {
                subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
            }
            if (subscriptionTogglHttpResponse == null) {
                subscriptionTogglHttpResponse = bus.Subscribe<TogglHttpResponseMessage> (OnTogglHttpResponse);
            }

            ResetRootViewController ();
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            Application.MarkLaunched ();
        }

        public override void ViewWillDisappear (bool animated)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionAuthChanged != null) {
                bus.Unsubscribe (subscriptionAuthChanged);
                subscriptionAuthChanged = null;
            }
            if (subscriptionTogglHttpResponse != null) {
                bus.Unsubscribe (subscriptionTogglHttpResponse);
                subscriptionTogglHttpResponse = null;
            }

            base.ViewWillDisappear (animated);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                if (subscriptionAuthChanged != null) {
                    bus.Unsubscribe (subscriptionAuthChanged);
                    subscriptionAuthChanged = null;
                }
                if (subscriptionTogglHttpResponse != null) {
                    bus.Unsubscribe (subscriptionTogglHttpResponse);
                    subscriptionTogglHttpResponse = null;
                }
            }
            base.Dispose (disposing);
        }

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            ResetRootViewController ();
        }

        private void OnTogglHttpResponse (TogglHttpResponseMessage msg)
        {
            if (msg.StatusCode == System.Net.HttpStatusCode.Gone) {
                if (upgradeAlert == null) {
                    upgradeAlert = new UIAlertView (
                        "MainUpdateNeededTitle".Tr (),
                        "MainUpdateNeededMessage".Tr (),
                        null, "MainUpdateNeededOk".Tr ());
                    upgradeAlert.Clicked += (s, e) => UIApplication.SharedApplication.OpenUrl (new NSUrl (Build.AppStoreUrl));
                }
                upgradeAlert.Show ();
            }
        }

        private void ResetRootViewController ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.IsAuthenticated && ViewControllers.Length < 1 ) {
                var vc = ViewControllers [0] is WelcomeViewController ? (UIViewController)new LogViewController () : new WelcomeViewController ();
                SetViewControllers (new [] { vc }, ViewControllers.Length > 0);
            }
        }

        private void OnEdgePanGesture ()
        {
            var progress = interactiveEdgePanGestureRecognizer.TranslationInView (View).X / View.Bounds.Width;
            progress = (float)Math.Min (1, Math.Max (0, progress));

            switch (interactiveEdgePanGestureRecognizer.State) {
            case UIGestureRecognizerState.Began:
                navDelegate.InteractiveTransition = new UIPercentDrivenInteractiveTransition ();
                PopViewController (true);
                break;
            case UIGestureRecognizerState.Changed:
                navDelegate.InteractiveTransition.UpdateInteractiveTransition (progress);
                break;
            case UIGestureRecognizerState.Ended:
            case UIGestureRecognizerState.Cancelled:
                if (progress > 0.5) {
                    navDelegate.InteractiveTransition.FinishInteractiveTransition ();
                } else {
                    navDelegate.InteractiveTransition.CancelInteractiveTransition ();
                }
                navDelegate.InteractiveTransition = null;
                break;
            }
        }

        private bool GestureRecognizerShouldBegin (UIGestureRecognizer recognizer)
        {
            // Make sure we're not mid transition or have too few view controllers
            var transitionCoordinator = this.GetTransitionCoordinator ();
            if (transitionCoordinator != null && transitionCoordinator.IsAnimated) {
                return false;
            }
            if (ViewControllers.Length <= 1) {
                return false;
            }

            var fromViewController = ViewControllers [ViewControllers.Length - 1];
            var toViewController = ViewControllers [ViewControllers.Length - 2];

            var fromDurationViewController = fromViewController as DurationChangeViewController;

            if (fromDurationViewController != null && fromDurationViewController.PreviousControllerType == toViewController.GetType ()) {
                if (recognizer == interactiveEdgePanGestureRecognizer) {
                    return true;
                }
            } else if (recognizer == InteractivePopGestureRecognizer) {
                return true;
            }

            return false;
        }

        private class NavDelegate : UINavigationControllerDelegate
        {
            public UIPercentDrivenInteractiveTransition InteractiveTransition { get; set; }

            public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForOperation (UINavigationController navigationController, UINavigationControllerOperation operation, UIViewController fromViewController, UIViewController toViewController)
            {
                if (toViewController is DurationChangeViewController) {
                    var durationController = (DurationChangeViewController)toViewController;
                    durationController.PreviousControllerType = fromViewController.GetType ();
                    return new DurationChangeViewController.PushAnimator ();
                }
                if (fromViewController is DurationChangeViewController) {
                    var durationController = (DurationChangeViewController)fromViewController;
                    if (durationController.PreviousControllerType == toViewController.GetType ()) {
                        return new DurationChangeViewController.PopAnimator ();
                    }
                    durationController.PreviousControllerType = null;
                }
                return null;
            }

            public override IUIViewControllerInteractiveTransitioning GetInteractionControllerForAnimationController (UINavigationController navigationController, IUIViewControllerAnimatedTransitioning animationController)
            {
                return InteractiveTransition;
            }
        }
    }
}
