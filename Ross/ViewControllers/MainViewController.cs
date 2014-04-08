using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class MainViewController : UIViewController
    {
        public MainViewController ()
        {
            View = new UILabel () {
                BackgroundColor = UIColor.White,
                TextAlignment = UITextAlignment.Center,
                Text = "Main",
            };
        }
    }
}
