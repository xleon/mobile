
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Migration
        {
            public static void TopLabel(UILabel v)
            {
                v.Font = UIFont.SystemFontOfSize(24f, UIFontWeight.Light);
                v.TextAlignment = UITextAlignment.Center;
                v.Lines = 2;
                v.TextColor = Color.DarkestGray;
            }

            public static void DescriptionLabel(UILabel v)
            {
                v.Font = UIFont.SystemFontOfSize(14f, UIFontWeight.UltraLight);
                v.TextAlignment = UITextAlignment.Center;
                v.Lines = 3;
                v.TextColor = Color.DarkestGray;
            }

            public static void ProgressBar(UIProgressView v)
            {
                v.TrackTintColor = Color.PinkishGrey;
                v.ProgressTintColor = Color.LightishGreen;
            }

            public static void TryAgainBtn(UIButton v)
            {
                v.Font = UIFont.SystemFontOfSize(16f, UIFontWeight.Medium);
                v.SetBackgroundImage(Color.LightishGreen.ToImage(), UIControlState.Normal);
                v.SetTitleColor(Color.White, UIControlState.Normal);
            }

            public static void DiscardTopLabel(UILabel v)
            {
                v.Font = UIFont.SystemFontOfSize(12f, UIFontWeight.Light);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.Steel;
            }

            public static void DiscardDescriptionLabel(UILabel v)
            {
                v.Font = UIFont.SystemFontOfSize(14f, UIFontWeight.Light);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.Steel;
                v.Lines = 3;
            }

            public static void DiscardBtn(UIButton v)
            {
                v.Font = UIFont.SystemFontOfSize(14f, UIFontWeight.Light);
                v.SetTitleColor(Color.DarkMint, UIControlState.Normal);
            }
        }
    }
}

