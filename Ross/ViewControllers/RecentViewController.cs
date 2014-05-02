using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class RecentViewController : BaseTimerTableViewController
    {
        private readonly NavigationMenuController navMenuController;

        public RecentViewController () : base (UITableViewStyle.Plain)
        {
            navMenuController = new NavigationMenuController ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            navMenuController.Attach (this);
        }
    }
}
