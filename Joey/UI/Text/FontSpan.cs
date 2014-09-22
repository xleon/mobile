using System;
using Android.Text.Style;
using Toggl.Joey.UI.Utils;
using Android.Text;
using Android.Graphics;

namespace Toggl.Joey.UI.Text
{
    public class FontSpan : MetricAffectingSpan
    {
        private readonly Font font;

        public FontSpan (Font font)
        {
            this.font = font;
        }

        public override void UpdateDrawState (TextPaint p)
        {
            ApplyFont (p);
        }

        public override void UpdateMeasureState (TextPaint p)
        {
            ApplyFont (p);
        }

        private void ApplyFont (TextPaint p)
        {
            var oldTypeface = p.Typeface;
            var oldStyle = oldTypeface != null ? oldTypeface.Style : TypefaceStyle.Normal;
            var fakeStyle = oldStyle & ~font.Typeface.Style;

            if ((fakeStyle & TypefaceStyle.Bold) != 0) {
                p.FakeBoldText = true;
            }

            if ((fakeStyle & TypefaceStyle.Italic) != 0) {
                p.TextSkewX = -0.25f;
            }

            p.SetTypeface (font.Typeface);
        }
    }
}
