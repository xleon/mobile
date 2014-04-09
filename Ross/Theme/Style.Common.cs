using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static readonly ViewStyler<UIView> Screen = (v) => {
            v.BackgroundColor = UIColor.LightGray;
        };
    }
}
