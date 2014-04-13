using System;
using MonoTouch.UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static readonly ViewStyler<UIView> Screen = (v) => {
            v.BackgroundColor = UIColor.White;
        };
        public static readonly ViewStyler<UINavigationBar> NavigationBar = (v) => {
            var borderImage = v.GetBorderImage ();
            if (borderImage != null) {
                borderImage.Hidden = true;
            }
        };
        public static readonly ViewStyler<TableViewHeaderView> TableViewHeader = (v) => {
            v.BackgroundColor = Color.LightGray;
        };
    }
}
