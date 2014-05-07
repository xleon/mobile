using System;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class EditTimeEntryViewController : UIViewController
    {
        public EditTimeEntryViewController (TimeEntryModel model)
        {
        }

        public override void LoadView ()
        {
            View = new UIView ().ApplyStyle (Style.Screen);
        }
    }
}
