using System;
using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    /// <summary>
    /// App view controller handles switching between login and main views automatically.
    /// </summary>
    public class AppViewController : UIViewController
    {
        private UIViewController content;
        private Subscription<AuthChangedMessage> subscriptionAuthChanged;

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
                if (!(RootViewController is MainViewController)) {
                    RootViewController = new MainViewController ();
                }
            } else {
                if (!(RootViewController is LoginViewController)) {
                    RootViewController = new LoginViewController ();
                }
            }
        }

        private UIViewController RootViewController {
            get { return content; }
            set {
                if (value == null)
                    throw new ArgumentNullException ("value");

                var oldContent = content;
                var newContent = content = value;

                if (oldContent == null) {
                    AddChildViewController (newContent);
                    newContent.View.Frame = View.Frame;
                    View.AddSubview (newContent.View);
                    newContent.DidMoveToParentViewController (this);
                } else {
                    oldContent.WillMoveToParentViewController (null);

                    AddChildViewController (newContent);
                    newContent.View.Frame = View.Frame;
                    newContent.View.Alpha = 0;

                    Transition (
                        oldContent,
                        newContent,
                        0.25,
                        UIViewAnimationOptions.TransitionNone,
                        () => {
                            oldContent.View.Alpha = 0.5f;
                            newContent.View.Alpha = 1;
                        },
                        (finished) => {
                            oldContent.RemoveFromParentViewController ();
                            newContent.DidMoveToParentViewController (this);
                        }
                    );
                }
            }
        }
    }
}
