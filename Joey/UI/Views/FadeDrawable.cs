using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;

namespace Toggl.Joey.UI.Views
{
    public class FadeDrawable : Drawable
    {
        private readonly int intrinsicWidth;
        private readonly StateColorMap colorMap = new StateColorMap ();
        private readonly Paint paint = new Paint ();
        private Color? gradientColor;

        public FadeDrawable (int intrinsicWidth)
        {
            this.intrinsicWidth = intrinsicWidth;
        }

        public void SetStateColor (int[] state, Color color)
        {
            colorMap.Add (state, color);
            OnStateChange (GetState ());
        }

        protected override bool OnStateChange (int[] state)
        {
            var color = colorMap.Get (state);
            if (color == null) {
                color = colorMap.Get (StateSet.WildCard.ToArray ());
            }

            if (gradientColor != color) {
                gradientColor = color;
                InvalidateGradient ();
                return true;
            }

            return base.OnStateChange (state);
        }

        protected override void OnBoundsChange (Rect bounds)
        {
            base.OnBoundsChange (bounds);
            InvalidateGradient ();
        }

        private void InvalidateGradient ()
        {
            if (gradientColor == null) {
                return;
            }

            var opaqueColor = gradientColor.Value;
            var transparentColor = new Color ((int)opaqueColor.R, opaqueColor.G, opaqueColor.B, 0);

            var gradient = new LinearGradient (
                Bounds.Left, Bounds.Top,
                Bounds.Right, Bounds.Top,
                transparentColor, opaqueColor,
                Shader.TileMode.Clamp);

            paint.SetShader (gradient);
        }

        public override void Draw (Canvas canvas)
        {
            if (gradientColor == null) {
                return;
            }

            canvas.DrawRect (Bounds, paint);
        }

        public override void SetAlpha (int alpha)
        {
        }

        public override void SetColorFilter (ColorFilter cf)
        {
        }

        public override int Opacity
        {
            get { return (int)Format.Translucent; }
        }

        public override int IntrinsicWidth
        {
            get { return intrinsicWidth; }
        }

        public override int IntrinsicHeight
        {
            get { return 1; }
        }

        public override bool IsStateful
        {
            get { return true; }
        }

        private class StateColorMap
        {
            private readonly List<int[]> stateSetList = new List<int[]> ();
            private readonly List<Color> colorList = new List<Color> ();

            public void Add (int[] state, Color color)
            {
                stateSetList.Add (state);
                colorList.Add (color);
            }

            public Color? Get (int[] state)
            {
                for (var i = 0; i < stateSetList.Count; i++) {
                    if (StateSet.StateSetMatches (stateSetList [i], state)) {
                        return colorList [i];
                    }
                }
                return null;
            }
        }

    }
}
