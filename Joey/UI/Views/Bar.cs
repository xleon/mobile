using System;
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Animation;

namespace Toggl.Joey.UI.Views
{
    public class Bar : View
    {
        private Rect barRectangle;
        private int widthValue;
        private Color mainColor = Color.ParseColor ("#00AEFF");
        private Color secondaryColor;
        private int animationSpeed = 1000;
        private int barHeight = 60;
        private int emptyState = 20;

        public Bar (Context context) : base (context)
        {
            Initialize ();
        }

        public Bar (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            Initialize ();
        }

        public Bar (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            Initialize ();
        }

        void Initialize ()
        {
            barRectangle = new Rect (0, 0, emptyState, barHeight);
            Console.WriteLine ("creating rect in INitialize");
            Invalidate ();
        }

        public override void Draw (Canvas canvas)
        {
//            Console.WriteLine ("Drawing again..");
            var RectPaint = new Paint ();
            RectPaint.Color = mainColor;
            canvas.DrawRect (barRectangle, RectPaint);
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            Console.WriteLine ("OnMeasure");
//            SetMeasuredDimension (MeasureSpec.GetSize (heightMeasureSpec), MeasureSpec.GetSize (widthMeasureSpec));
            SetMeasuredDimension (500, 40);

        }

        public void StartAnimate ()
        {
//            Console.WriteLine ("Value: {0}", Value);
            var animator = ValueAnimator.OfInt (emptyState, Value);
            animator.SetDuration (animationSpeed);
            animator.Update += (sender, e) => RectWidth = (int)e.Animation.AnimatedValue;
            animator.Start ();
        }

        public int Value {
            get {
                return widthValue;
            }
            set {
                this.widthValue = value;
                StartAnimate ();
            }
        }

        public int RectWidth {
            get {
                return barRectangle.Right;
            }
            set {
                barRectangle.Right = value;
//                Console.WriteLine ("changing rect width: {0}", value);
                Invalidate ();
            }
        }
    }
}