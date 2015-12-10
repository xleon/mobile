using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class LeftView
        {
            public static void Button (UIButton v)
            {
                v.SetTitleColor (UIColor.Black, UIControlState.Normal);
                v.Font = UIFont.FromName ("HelveticaNeue", 14f);
            }

            public static void UserLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 17f);
                v.TextAlignment = UITextAlignment.Left;
            }
        }
    }
}