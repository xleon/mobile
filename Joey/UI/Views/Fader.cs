using System;
using Android.Widget;
using Android.Content;
using Android.Util;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public class Fader : Drawable
    {
        private readonly Paint fadePaint = new Paint ();
        private readonly Rect fadeRect = new Rect ();
        public bool EnableFade = false;

        public override void Draw(Canvas canvas)
        {
            fadeRect.Right = canvas.Width;
            fadeRect.Bottom = canvas.Height;

            fadeRect.Left = canvas.Width - 50;
            fadeRect.Top = 0;

            var gradient = new LinearGradient (
                fadeRect.Left, fadeRect.Top,
                fadeRect.Right, fadeRect.Top,
                new Color (255, 255, 255, 255), new Color (255, 255, 255, 0),
                Shader.TileMode.Clamp);
            fadePaint.SetShader (gradient);

            canvas.DrawRect (fadeRect, fadePaint);
        }

        public override void SetAlpha (int alpha)
        {
        }

        public override void SetColorFilter(ColorFilter cf)
        {
//            base.SetColorFilter (cf);
        }
        public override int Opacity {
            get {
                return 255;
            }
        }
//        public override int GetOpacity ()
//        {
//            return 255;
//        }
    }
}
