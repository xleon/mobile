using System;
using Android.Widget;
using Android.Graphics;
using Android.Content;
using Toggl.Joey.UI.Utils;

namespace Toggl.Joey.UI.Views
{
    public static class TextViewExtensions
    {
        public static T SetFont<T> (this T view, Font font)
        where T : TextView
        {
            view.SetTypeface (font.Typeface, font.Style);
            return view;
        }
    }
}
