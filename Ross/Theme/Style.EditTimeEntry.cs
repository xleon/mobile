using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class EditTimeEntry
        {
            public static void DateLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 13f);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.Gray;
            }

            public static void DateLabelActive (UILabel v)
            {
                v.ApplyStyle (DateLabel);
                v.TextColor = Color.Red;
            }

            public static void TimeLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 13f);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.Black;
            }

            public static void TimeLabelActive (UILabel v)
            {
                v.ApplyStyle (TimeLabel);
                v.TextColor = Color.Red;
            }
        }
    }
}
