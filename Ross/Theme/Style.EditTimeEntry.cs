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

            public static void DatePicker (UIDatePicker v)
            {
                v.BackgroundColor = Color.White;
            }

            public static void ProjectLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Medium", 17f);
                v.TextColor = Color.White;
            }

            public static void ClientLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 13f);
                v.TextColor = Color.White.ColorWithAlpha (0.75f);
            }

            public static void TaskLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 13f);
                v.TextColor = Color.White.ColorWithAlpha (0.75f);
            }
        }
    }
}
