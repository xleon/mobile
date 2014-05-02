using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;

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

            NavigationBar.ApplyStyle (Style.NavigationBar);
        }
    }
}
