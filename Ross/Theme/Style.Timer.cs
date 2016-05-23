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
            }

            public static void TimerModeSwitchLabel(UILabel v)
            {
                v.TextColor = Color.TextInactive;
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
            }
        }

    }
}

