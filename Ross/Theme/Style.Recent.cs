using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Recent
        {
            public static void HeaderBackgroundView (UIView v)
            {
                v.BackgroundColor = Color.LightGray;
            }

            public static void HeaderLabel (UILabel v)
            {
                v.TextColor = Color.Gray;
                v.TextAlignment = UITextAlignment.Left;
                v.Font = UIFont.FromName ("HelveticaNeue-Medium", 14f);
            }

            public static void CellContentView (UIView v)
            {
                v.ApplyStyle (TimeEntryCell.ContentView);
            }

            public static void CellProjectLabel (UILabel v)
            {
                v.ApplyStyle (TimeEntryCell.ProjectLabel);
            }

            public static void CellClientLabel (UILabel v)
            {
                v.ApplyStyle (TimeEntryCell.ClientLabel);
            }

            public static void CellTaskLabel (UILabel v)
            {
                v.ApplyStyle (TimeEntryCell.TaskLabel);
            }

            public static void CellDescriptionLabel (UILabel v)
            {
                v.ApplyStyle (TimeEntryCell.DescriptionLabel);
            }

            public static void CellDurationLabel (UILabel v)
            {
                v.ApplyStyle (TimeEntryCell.DurationLabel);
            }

            public static void CellTaskDescriptionSeparator (UIImageView v)
            {
                v.ApplyStyle (TimeEntryCell.TaskDescriptionSeparator);
            }

            public static void CellRunningIndicator (UIImageView v)
            {
                v.ApplyStyle (TimeEntryCell.RunningIndicator);
            }
        }
    }
}
