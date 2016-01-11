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
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 23f);
                v.TitleEdgeInsets = new UIEdgeInsets (0, 50f, 0, 0);
                v.ImageEdgeInsets = new UIEdgeInsets (0, 20f, 0, 20f);
                v.ContentEdgeInsets = new UIEdgeInsets (13f, 0, 13f, 0);
            }

            public static void UserLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 17f);
                v.TextAlignment = UITextAlignment.Left;
            }
        }
    }
}