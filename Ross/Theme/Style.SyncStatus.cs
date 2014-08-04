using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class SyncStatus
        {
            public static void BarBackground (UIView v)
            {
                v.BackgroundColor = Color.Black.ColorWithAlpha (0.85f);
            }

            public static void StatusLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 14f);
                v.TextColor = Color.White;
            }

            public static void RetryButton (UIButton v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.SetImage (Image.IconRetry, UIControlState.Normal);
            }

            public static void CancelButton (UIButton v)
            {
                v.SetImage (Image.IconCancel, UIControlState.Normal);
            }
        }
    }
}
