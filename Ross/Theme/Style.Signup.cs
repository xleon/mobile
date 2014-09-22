using System;
using MonoTouch.TTTAttributedLabel;
using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Signup
        {
            public static void EmailField (UITextField v)
            {
                v.TextColor = Color.Black;
            }

            public static void PasswordField (UITextField v)
            {
                v.TextColor = Color.Black;
            }

            public static void SignupButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 20f);
                v.SetBackgroundImage (Color.Green.ToImage (), UIControlState.Normal);
                v.SetTitleColor (Color.White, UIControlState.Normal);
            }

            public static void GoogleButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 16f);
                v.SetBackgroundImage (UIColor.Clear.ToImage (), UIControlState.Normal);
                v.SetBackgroundImage (UIColor.FromWhiteAlpha (0f, 0.3f).ToImage (), UIControlState.Highlighted);
                v.SetTitleColor (Color.Gray, UIControlState.Normal);
            }

            public static void InputsContainer (UIView v)
            {
                v.BackgroundColor = Color.White;
            }

            public static void InputsBorder (UIView v)
            {
                v.BackgroundColor = Color.Gray.ColorWithAlpha (0.5f);
            }

            public static void LegalLabel (TTTAttributedLabel v)
            {
                v.Lines = 2;
                v.Font = UIFont.FromName ("HelveticaNeue", 16f);
                v.TextColor = Color.Gray;
                v.TextAlignment = UITextAlignment.Center;
                v.LinkAttributes = new UIStringAttributes () {
                    ForegroundColor = Color.Green,
                    ParagraphStyle = new NSMutableParagraphStyle () {
                        Alignment = UITextAlignment.Center,
                    },
                } .Dictionary;
                v.ActiveLinkAttributes = new UIStringAttributes () {
                    ForegroundColor = Color.DarkGreen,
                } .Dictionary;
            }
        }
    }
}
