using System;
using MonoTouch.UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class NewProject
        {
            public static void NameField (TextField v)
            {
                v.BackgroundColor = Color.White;
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 17f);
                v.TextColor = Color.Black;
                v.TextEdgeInsets = new UIEdgeInsets (0, 15f, 0, 15f);
            }

            public static void ClientButton (UIButton v)
            {
                v.SetBackgroundImage (Color.White.ToImage (), UIControlState.Normal);
                v.SetBackgroundImage (Color.LightestGray.ToImage (), UIControlState.Highlighted);
                v.ContentEdgeInsets = new UIEdgeInsets (0, 15f, 0, 15f);
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 17f);
                v.HorizontalAlignment = UIControlContentHorizontalAlignment.Fill;
            }

            public static void NoClient (UIButton v)
            {
                v.SetTitleColor (Color.Gray, UIControlState.Normal);
            }

            public static void WithClient (UIButton v)
            {
                v.SetTitleColor (Color.Black, UIControlState.Normal);
            }
        }
    }
}

