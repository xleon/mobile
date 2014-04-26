using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Log
        {
            public static void HeaderBackgroundView (UIView v)
            {
                v.BackgroundColor = Color.LightGray;
            }

            public static void HeaderDateLabel (UILabel v)
            {
                v.TextColor = Color.Gray;
                v.TextAlignment = UITextAlignment.Left;
                v.Font = UIFont.FromName ("HelveticaNeue-Medium", 14f);
            }

            public static void HeaderDurationLabel (UILabel v)
            {
                v.TextColor = Color.Gray;
                v.TextAlignment = UITextAlignment.Right;
                v.Font = UIFont.FromName ("HelveticaNeue", 14f);
            }

            public static void CellProjectLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Medium", 18f);
            }

            public static void CellClientLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 15f);
                v.TextColor = UIColor.Gray;
            }

            public static void CellTaskLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Bold", 14f);
            }

            public static void CellDescriptionLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue", 14f);
            }

            public static void CellDurationLabel (UILabel v)
            {
                v.TextAlignment = UITextAlignment.Right;
                v.Font = UIFont.FromName ("HelveticaNeue-Light", 18f);
            }

            public static void CellTaskDescriptionSeparator (UIImageView v)
            {
            }

            public static void CellRunningIndicator (UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Images.IconRunning;
            }

            public static void BillableAndTaggedEntry (UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Images.IconTagBillable;
            }

            public static void TaggedEntry (UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Images.IconTag;
            }

            public static void BillableEntry (UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Images.IconBillable;
            }

            public static void PlainEntry (UIImageView v)
            {
                v.Image = null;
            }
        }
    }
}
