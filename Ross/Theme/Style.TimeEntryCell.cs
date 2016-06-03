using System;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class TimeEntryCell
        {
            public const float ProjectCircleRadius = 4;
            public const float DescriptionTaskSeparatorRadius = 1;
            public const float RunningIndicatorRadius = 9;
            public const float IconSize = 24;

            private static readonly UIFont sharedFont = Font.Main(15);

            public static void ContentView(UIView v)
            {
                v.Opaque = true;
                v.BackgroundColor = Color.White;
            }

            public static void SwipeActionButton(UIButton v)
            {
                v.SetTitleColor(Color.White, UIControlState.Normal);
                v.Font = UIFont.FromName("HelveticaNeue", 18f);
                v.TitleLabel.TextAlignment = UITextAlignment.Center;
            }

            public static void BillableImage(UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Image.IconBillable;
            }

            public static void TagsImage(UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Image.IconTag;
            }

            public static void ProjectCircle(UIView v)
            {
                v.Layer.CornerRadius = ProjectCircleRadius;
            }

            public static void DescriptionTaskSeparator(UIView v)
            {
                v.Layer.CornerRadius = DescriptionTaskSeparatorRadius;
                v.BackgroundColor = Color.OffBlack;
            }

            public static void ProjectLabel(UILabel v)
            {
                v.Font = sharedFont;
                v.TextColor = Color.OffBlack;
            }

            public static void ClientLabel(UILabel v)
            {
                v.Font = sharedFont;
                v.TextColor = Color.OffSteel;
            }

            public static void TaskLabel(UILabel v)
            {
                v.Font = sharedFont;
                v.TextColor = Color.OffSteel;
            }

            public static void DescriptionLabel(UILabel v)
            {
                v.Font = sharedFont;
                v.TextColor = Color.OffBlack;
            }

            public static void DurationLabel(UILabel v)
            {
                v.TextAlignment = UITextAlignment.Right;
                v.Font = sharedFont;
                v.TextColor = Color.OffSteel;
            }

            public static void RunningIndicator(UIView v)
            {
                v.BackgroundColor = Color.StopButton;
                v.Layer.CornerRadius = RunningIndicatorRadius;
            }

            public static void RunningIndicatorPointer(CAShapeLayer l)
            {
                l.StrokeColor = Color.White.CGColor;
                l.LineWidth = 1.5f;
                l.LineCap = CAShapeLayer.CapRound;

                var path = new CGPath();

                var r = RunningIndicatorRadius;

                path.MoveToPoint(new CGPoint(0, 0));
                path.AddLineToPoint(new CGPoint(0, 4 - r));

                l.Path = path;
            }

            public static void ContinueState(UIView v)
            {
                v.BackgroundColor = Color.Green;
            }

            public static void NoSwipeState(UIView v)
            {
                v.BackgroundColor = UIColor.Clear;
            }

        }
    }
}
