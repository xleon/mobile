using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class LoginViewController : UIViewController
    {
        public LoginViewController ()
        {
        }

        public override void LoadView ()
        {
            View = new UILabel () {
                BackgroundColor = UIColor.White,
                TextAlignment = UITextAlignment.Center,
                Text = "Login",
            };
        }
    }
}
