using System;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Timer
        {
            public static void StartButtonCircle(UIView v)
            {
                v.BackgroundColor = Color.StartButton;
                v.Layer.CornerRadius = 20;
                v.ClipsToBounds = true;
            }

            public static void TimerModeSwitchLabelInactive(UILabel v)
            {
                v.TextColor = Color.TextInactive;
                v.Font = Font.Main(12);
            }
            public static void TimerModeSwitchLabelTimer(UILabel v)
            {
                v.TextColor = Color.StartButton;
                v.Font = Font.Main(12);
            }
            public static void TimerModeSwitchLabelManual(UILabel v)
            {
                v.TextColor = Color.AddManualButton;
                v.Font = Font.Main(12);
            }

            public static void TimerModeSwitch(UISwitch v)
            {
                v.OnTintColor = Color.AddManualButton;
                v.TintColor = Color.StartButton;
                v.BackgroundColor = Color.StartButton;
                v.Transform = new CoreGraphics.CGAffineTransform(0.54f, 0f, 0f, 0.54f, 0, 0);

                v.Layer.CornerRadius = 18;
            }

            public static void TimerDurationLabel(UILabel v)
            {
                v.Text = "00:00:00";
                v.Font = Font.MainLight(24);
                v.TextColor = Color.OffSteel;
                v.TextAlignment = UITextAlignment.Right;
            }


            public static void StartButtonHighlight(CALayer l)
            {
                l.Frame = new CGRect(0, 0, 40, 40);
                l.BackgroundColor = Color.Black.CGColor;
                l.Opacity = 0.2f;
                l.Hidden = true;
            }
        }

    }
}

