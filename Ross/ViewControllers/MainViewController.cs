using MonoTouch.Foundation;
using MonoTouch.UIKit;
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
        private UIAlertView upgradeAlert;

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            NavigationBar.Apply (Style.NavigationBar);
            Delegate = new NavDelegate ();
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
            if (authManager.IsAuthenticated) {
                if (ViewControllers.Length < 1 || ViewControllers [0] is WelcomeViewController) {
                    // Determine the default root view controller
                    UIViewController activeController;
                    var preferredView = ServiceContainer.Resolve<SettingsStore> ().PreferredStartView;
                    if (preferredView == "recent") {
                        activeController = new RecentViewController ();
                    } else {
                        activeController = new LogViewController ();
                    }

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
