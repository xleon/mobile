using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Settings
        {
            public static void RowBackground<T> (T v)
            where T : UIView
            {
                v.BackgroundColor = Color.White;
            }

            public static void Separator<T> (T v)
            where T : UIView
            {
                v.BackgroundColor = Color.Gray.ColorWithAlpha(0.1f);
            }

            public static void SettingLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 17f);
                v.TextAlignment = UITextAlignment.Left;
                v.TextColor = Color.Black;
            }

            public static void DescriptionLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 13f);
                v.Lines = 3;
                v.TextAlignment = UITextAlignment.Left;
                v.TextColor = Color.Gray;
            }
        }
    }
}
