using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Welcome
        {
            public static void Background(UIImageView v)
            {
                v.Image = Image.LoginBackground;
                v.ContentMode = UIViewContentMode.ScaleAspectFill;
            }

            public static void Logo(UIImageView v)
            {
                v.Image = Image.Logo;
                v.ContentMode = UIViewContentMode.Center;
            }

            public static void Slogan(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 18f);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.White;
            }

            public static void CreateAccount(UIButton v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Light", 20f);
                v.SetBackgroundImage(Color.DarkRed.ToImage(), UIControlState.Normal);
                v.SetTitleColor(Color.White, UIControlState.Normal);
            }

            public static void PasswordLogin(UIButton v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Light", 20f);
                v.SetBackgroundImage(Color.White.ToImage(), UIControlState.Normal);
                v.SetTitleColor(Color.DarkRed, UIControlState.Normal);
            }

            public static void GoogleLogin(UIButton v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 16f);
                v.SetBackgroundImage(UIColor.Clear.ToImage(), UIControlState.Normal);
                v.SetBackgroundImage(UIColor.FromWhiteAlpha(0f, 0.3f).ToImage(), UIControlState.Highlighted);
                v.SetTitleColor(Color.White, UIControlState.Normal);
            }
        }
    }
}
