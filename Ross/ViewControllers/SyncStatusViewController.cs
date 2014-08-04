using System;
using System.Drawing;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public abstract class SyncStatusViewController : UIViewController
    {
        private readonly UIViewController contentViewController;

        protected SyncStatusViewController (UIViewController viewController)
        {
            if (viewController == null)
                throw new ArgumentNullException ("viewController");

            contentViewController = viewController;
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            // Add content view controller:
            AddChildViewController (contentViewController);
            contentViewController.View.Frame = new RectangleF (PointF.Empty, View.Frame.Size);
            View.AddSubview (contentViewController.View);
            contentViewController.DidMoveToParentViewController (this);
        }

        public override UINavigationItem NavigationItem {
            get { return contentViewController.NavigationItem; }
        }
    }
}
