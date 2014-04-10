using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class MainViewController : UINavigationController
    {
        public MainViewController ()
        {
            UIViewController activeController;
            activeController = new LogViewController ();
            ViewControllers = new UIViewController[] { activeController };
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
        }
    }
}
