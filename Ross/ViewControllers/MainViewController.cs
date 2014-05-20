using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class MainViewController : UINavigationController
    {
        private Subscription<AuthChangedMessage> subscriptionAuthChanged;

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            NavigationBar.Apply (Style.NavigationBar);
            Delegate = new NavDelegate ();
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            if (subscriptionAuthChanged == null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
            }

            ResetRootViewController ();
        }

        public override void ViewWillDisappear (bool animated)
        {
            if (subscriptionAuthChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionAuthChanged);
                subscriptionAuthChanged = null;
            }

            base.ViewWillDisappear (animated);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (subscriptionAuthChanged != null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    bus.Unsubscribe (subscriptionAuthChanged);
                    subscriptionAuthChanged = null;
                }
            }
            base.Dispose (disposing);
        }

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            ResetRootViewController ();
        }

        private void ResetRootViewController ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.IsAuthenticated) {
                if (ViewControllers.Length < 1 || ViewControllers [0] is WelcomeViewController) {
                    // TODO: Determine the default root view controller
                    UIViewController activeController;
                    activeController = new LogViewController ();

                    SetViewControllers (new [] { activeController }, ViewControllers.Length > 0);
                }
            } else {
                if (ViewControllers.Length < 1 || !(ViewControllers [0] is WelcomeViewController)) {
                    SetViewControllers (new [] { new WelcomeViewController () }, ViewControllers.Length > 0);
                }
            }
        }

        private class NavDelegate : UINavigationControllerDelegate
        {
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
        }
    }
}
