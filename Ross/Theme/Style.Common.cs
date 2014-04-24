using System;
using MonoTouch.UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static void Screen (UIView v)
        {
            v.BackgroundColor = UIColor.White;
        }

        public static void NavigationBar (UINavigationBar v)
        {
            var borderImage = v.GetBorderImage ();
            if (borderImage != null) {
                borderImage.Hidden = true;
            }
        }

        public static void TableViewHeader (TableViewHeaderView v)
        {
            v.BackgroundColor = Color.LightGray;
        }
    }
}
