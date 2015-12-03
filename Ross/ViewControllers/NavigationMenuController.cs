using System;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class NavigationMenuController
    {
        private TogglWindow window;

        public void Attach (UIViewController controller)
        {
            controller.NavigationItem.LeftBarButtonItem = new UIBarButtonItem (
                Image.IconNav.ImageWithRenderingMode (UIImageRenderingMode.AlwaysOriginal),
                UIBarButtonItemStyle.Plain, OnNavigationButtonTouched);

            window = AppDelegate.TogglWindow;
        }

        private void OnNavigationButtonTouched (object sender, EventArgs e)
        {
            var main = window.RootViewController as MainViewController;
            if (main != null) {
                main.ToggleMenu ();
            }
        }

        public void Detach ()
        {
            window = null;
        }
    }
}