using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public abstract class BaseTimerTableViewController : UITableViewController
    {
        private readonly TimerNavigationController timerController;

        protected BaseTimerTableViewController (UITableViewStyle withStyle) : base (withStyle)
        {
            timerController = new TimerNavigationController ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            timerController.Attach (this);
        }

        public override void ViewWillAppear (bool animated)
        {
            timerController.Start ();
            base.ViewWillAppear (animated);
        }

        public override void ViewDidDisappear (bool animated)
        {
            timerController.Stop ();
            base.ViewDidDisappear (animated);
        }
    }
}
