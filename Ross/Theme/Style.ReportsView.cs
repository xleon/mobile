using System;
using MonoTouch.UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class ReportsView
        {
            public static void SelectorButton (UIButton v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Thin", 18f);
                v.LineBreakMode = UILineBreakMode.Clip;
                v.SetTitleColor (Color.Black, UIControlState.Normal);
                v.SetTitleColor (Color.Gray, UIControlState.Highlighted);
            }

            public static void DateSelectorLabel (UILabel v)
            {
                v.Font = UIFont.FromName ("HelveticaNeue-Thin", 20f);
                v.LineBreakMode = UILineBreakMode.Clip;
                v.TextColor = Color.Gray;
                v.BackgroundColor = UIColor.Clear;
                v.TextAlignment = UITextAlignment.Center;
            }

            public static void DateSelectorLeftArrowButton (UIButton v)
            {
                v.SetTitle (String.Empty, UIControlState.Normal);
                v.SetImage ( UIImage.FromFile ( "btn-arrow-left.png"), UIControlState.Normal);
            }

            public static void DateSelectorRightArrowButton (UIButton v)
            {
                v.SetTitle (String.Empty, UIControlState.Normal);
                v.SetImage ( UIImage.FromFile ( "btn-arrow-right.png"), UIControlState.Normal);
            }
        }
    }
}
