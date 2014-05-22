using System;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class SettingsViewController : UIViewController
    {
        public SettingsViewController ()
        {
            Title = "SettingsTitle".Tr ();
        }

        public override void LoadView ()
        {
            View = new UIView ().Apply (Style.Screen);
        }
    }
}
