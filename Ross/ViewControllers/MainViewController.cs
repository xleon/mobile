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

            NavigationBar.ApplyStyle (Style.NavigationBar);
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
    }
}
