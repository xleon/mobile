using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class LoginViewController : UIViewController
    {
        public override void LoadView ()
        {
            var label = new UILabel () {
                Text = "LoginHeaderText".Tr (),
            };
            View = label;
        }
    }
}
