using System;
using UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class DurationEdit
        {
            public static void DurationView(DurationView v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Thin", 32f);
                v.TextColor = Color.Gray;
                v.HighlightedTextColor = Color.Black;
            }
        }
    }
}
