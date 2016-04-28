
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Migration
        {
            public static void Text(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 18f);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.DarkGray;
            }

            public static void ProgressBar(UIProgressView v)
            {
                v.TrackTintColor = Color.LightestGray;
                v.ProgressTintColor = Color.DarkRed;
            }
        }
    }
}

