using System;
using Android.Widget;
using Android.Content;
using Android.Util;
using Android.Graphics;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public class FadeableText : TextView
    {
        private readonly Paint fadePaint = new Paint ();
        private readonly Rect fadeRect = new Rect ();
        public bool EnableFade = false;

        public FadeableText (Context context) : base (context)
        {
        }

        public FadeableText (Context ctx, IAttributeSet attrs) : base (ctx, attrs)
        {
            var a = ctx.ObtainStyledAttributes (attrs, Resource.Styleable.FadeView);
            FadeLength = 50;
            a.Recycle ();

            fadePaint.SetXfermode (new PorterDuffXfermode (PorterDuff.Mode.DstIn));
        }

        public FadeableText (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        protected override void DispatchDraw (Canvas canvas)
        {
            base.DispatchDraw (canvas);

            return;

            if (!EnableFade)
                return;

            fadeRect.Right = canvas.Width;
            fadeRect.Bottom = canvas.Height;

            fadeRect.Left = canvas.Width - FadeLength;
            fadeRect.Top = 0;
 
            var gradient = new LinearGradient (
                               fadeRect.Left, fadeRect.Top,
                               fadeRect.Right, fadeRect.Top,
                               new Color (255, 255, 255, 255), new Color (255, 255, 255, 0),
                               Shader.TileMode.Clamp);
            fadePaint.SetShader (gradient);

            canvas.DrawRect (fadeRect, fadePaint);
        }

        private int fadeLength;

        public int FadeLength {
            get { return fadeLength; }
            set {
                if (fadeLength == value)
                    return;
                fadeLength = value;

                var fadeEnabled = fadeLength > 0;
                SetLayerType (fadeEnabled ? LayerType.Hardware : LayerType.None, null);
                Invalidate ();
            }
        }

    }
}

