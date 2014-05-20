using System;
using MonoTouch.UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class NavTimer
        {
            public static void DurationButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Thin", 32f);
                v.LineBreakMode = UILineBreakMode.Clip;
                v.SetTitleColor (UIColor.Black, UIControlState.Normal);
                v.SetTitleColor (UIColor.Gray, UIControlState.Highlighted);
            }

            public static void StartButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 16f);
                v.SetBackgroundImage (Image.CircleStart, UIControlState.Normal);
                v.SetBackgroundImage (Image.CircleStartPressed, UIControlState.Highlighted);
                v.SetTitleColor (Color.White, UIControlState.Normal);
                v.SetTitleColor (Color.White, UIControlState.Highlighted);
                v.SetTitle ("NavTimerStart".Tr (), UIControlState.Normal);
                // TODO: Remove this scale workaround
                v.Transform = MonoTouch.CoreGraphics.CGAffineTransform.MakeScale (0.7f, 0.7f);
            }

            public static void StopButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 16f);
                v.SetBackgroundImage (Image.CircleStop, UIControlState.Normal);
                v.SetBackgroundImage (Image.CircleStopPressed, UIControlState.Highlighted);
                v.SetTitleColor (Color.Red, UIControlState.Normal);
                v.SetTitleColor (Color.White, UIControlState.Highlighted);
                v.SetTitle ("NavTimerStop".Tr (), UIControlState.Normal);
                // TODO: Remove this scale workaround
                v.Transform = MonoTouch.CoreGraphics.CGAffineTransform.MakeScale (0.7f, 0.7f);
            }
        }
    }
}
