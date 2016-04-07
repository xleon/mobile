using System;
using UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class NavMenu
        {
            public static void Background(UIView v)
            {
                v.BackgroundColor = Color.Black.ColorWithAlpha(0.90f);
            }

            public static void Separator(UIView v)
            {
                v.BackgroundColor = Color.DarkGray;
            }

            public static void MenuItem(UIButton v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Thin", 30f);
                v.ContentEdgeInsets = new UIEdgeInsets(13f, 22f, 13f, 22f);
                v.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
            }

            public static void HighlightedItem(UIButton v)
            {
                v.Apply(MenuItem);
                v.SetTitleColor(Color.White, UIControlState.Normal);
                v.SetTitleColor(Color.White, UIControlState.Highlighted);
            }

            public static void NormalItem(UIButton v)
            {
                v.Apply(MenuItem);
                v.SetTitleColor(Color.Gray, UIControlState.Normal);
                v.SetTitleColor(Color.White, UIControlState.Highlighted);
            }
        }
    }
}
