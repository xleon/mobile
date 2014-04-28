using System;
using MonoTouch.UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class NavTimer
        {
            public static void DurationLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Thin", 32f);
                v.TextAlignment = UITextAlignment.Center;
                v.LineBreakMode = UILineBreakMode.Clip;
            }

            public static void StartButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 16f);
                v.SetBackgroundImage (Image.CircleStart, UIControlState.Normal);
                v.SetTitleColor (Color.White, UIControlState.Normal);
                v.SetTitle ("NavTimerStart".Tr (), UIControlState.Normal);
            }

            public static void StopButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 16f);
                v.SetBackgroundImage (Image.CircleStop, UIControlState.Normal);
                v.SetTitleColor (Color.Red, UIControlState.Normal);
                v.SetTitle ("NavTimerStop".Tr (), UIControlState.Normal);
            }
        }
    }
}
