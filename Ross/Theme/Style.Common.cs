using System;
using MonoTouch.UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static void Screen (UIView v)
        {
            v.BackgroundColor = Color.LightGray;
        }

        public static void NavigationBar (UINavigationBar v)
        {
            var borderImage = v.GetBorderImage ();
            if (borderImage != null) {
                borderImage.Hidden = true;
            }
        }

        public static void NavLabelButton (UIBarButtonItem v)
        {
            v.SetTitleTextAttributes (new UITextAttributes () {
                Font = UIFont.FromName ("HelveticaNeue-Medium", 17f),
                TextColor = Color.Green,
            }, UIControlState.Normal);
        }

        public static void TableViewHeader (TableViewHeaderView v)
        {
            v.BackgroundColor = Color.LightGray;
        }

        public static void CellSelectedBackground (UIView v)
        {
            v.BackgroundColor = Color.LightGray;
        }
    }
}
