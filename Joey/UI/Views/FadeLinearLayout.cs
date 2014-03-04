using System;
using Android.Widget;
using Android.Content;
using Android.Util;
using Android.Graphics;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public class FadeLinearLayout : LinearLayout
    {
        private readonly Paint fadePaint = new Paint ();
        private readonly Rect fadeRect = new Rect ();

        public FadeLinearLayout (Context ctx) : this (ctx, null, 0)
        {
        }

        public FadeLinearLayout (Context ctx, IAttributeSet attrs) : this (ctx, attrs, 0)
        {
        }

        public FadeLinearLayout (Context ctx, IAttributeSet attrs, int defStyle) : base (ctx, null, defStyle)
        {
            var a = ctx.ObtainStyledAttributes (attrs, Resource.Styleable.FadeLinearLayout);
            var count = a.IndexCount;
            for (var i = 0; i < count; i++) {
                int attr = a.GetIndex (i);
                switch (attr) {
                case Resource.Styleable.FadeLinearLayout_fadeLength:
                    FadeLength = a.GetDimensionPixelSize (attr, FadeLength);
                    break;
                }
            }
            a.Recycle ();

            fadePaint.SetXfermode (new PorterDuffXfermode (PorterDuff.Mode.DstIn));
        }

        public FadeLinearLayout (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        protected override void DispatchDraw (Canvas canvas)
        {
            base.DispatchDraw (canvas);

            if (FadeLength > 0) {
                var isHoriz = Orientation == Orientation.Horizontal;

                fadeRect.Right = canvas.Width;
                fadeRect.Bottom = canvas.Height;

                if (isHoriz) {
                    // On the right
                    fadeRect.Left = canvas.Width - FadeLength;
                    fadeRect.Top = 0;
                } else {
                    // On the bottom
                    fadeRect.Left = 0;
                    fadeRect.Top = canvas.Height - FadeLength;
                }

                var gradient = new LinearGradient (
                                   fadeRect.Left, fadeRect.Top,
                                   isHoriz ? fadeRect.Right : fadeRect.Left, isHoriz ? fadeRect.Top : fadeRect.Bottom,
                                   new Color (255, 255, 255, 255), new Color (255, 255, 255, 0),
                                   Shader.TileMode.Clamp);
                fadePaint.SetShader (gradient);

                canvas.DrawRect (fadeRect, fadePaint);
            }
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

