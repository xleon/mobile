using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;

namespace Toggl.Joey.UI.Views
{
    public class NotificationImageView : ImageView
    {
        private Context ctx;
        private int bubbleCount = 0;
        private Drawable backgroundShape;
        private Paint paint;
        private Rect textBoundsRect = new Rect ();

        public int BubbleCount
        {
            get {
                return bubbleCount;
            } set {
                bubbleCount = value;
                Invalidate ();
            }
        }

        private Drawable CircleShape
        {
            get {
                if (backgroundShape == null) {
                    backgroundShape = ctx.Resources.GetDrawable (Resource.Drawable.NotificationBubble);
                }
                return backgroundShape;
            }
        }

        private Paint LabelPaint
        {
            get {
                if (paint == null) {
                    var labelFontSize = TypedValue.ApplyDimension (ComplexUnitType.Dip, 9, ctx.Resources.DisplayMetrics);
                    paint = new Paint {
                        Color = ctx.Resources.GetColor (Resource.Color.dark_gray_text),
                        TextSize = labelFontSize,
                        AntiAlias = true,
                    };
                }
                return paint;
            }
        }

        private string BubbleText
        {
            get {
                return BubbleCount.ToString ();
            }
        }

        public NotificationImageView (Context context) : base (context)
        {
            ctx = context;
        }

        public NotificationImageView (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            ctx = context;
        }

        public NotificationImageView (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            ctx = context;
        }

        public override void Draw (Canvas canvas)
        {
            base.Draw (canvas);

            var circleX = canvas.Width /2 - CircleShape.MinimumWidth /4;
            var circleY = canvas.Height /2 - CircleShape.MinimumHeight;
            CircleShape.SetBounds (circleX, circleY, circleX + CircleShape.MinimumWidth, circleY + CircleShape.MinimumHeight);
            CircleShape.Draw (canvas);

            LabelPaint.GetTextBounds (BubbleText, 0, BubbleText.Length, textBoundsRect);
            canvas.DrawText (BubbleText,
                             circleX + CircleShape.MinimumWidth /2 - textBoundsRect.Width () /2,
                             (canvas.Height - textBoundsRect.Height ())/2, LabelPaint);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                CircleShape.Dispose ();
                LabelPaint.Dispose ();
                textBoundsRect.Dispose ();
            }
            base.Dispose (disposing);
        }
    }
}

