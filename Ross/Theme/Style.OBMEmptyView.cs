using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class OBMEmptyView
        {
            public static void TitleLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Light", 35f);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.LightGray;
            }

            public static void MessageLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Light", 17f);
                v.Lines = 5;
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.LightGray;
            }

            public static void ArrowImageView(UIImageView v)
            {
                v.Image = Image.ArrowEmptyState;
            }
        }
    }
}
