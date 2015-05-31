using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Toggl.Joey.UI.Views
{
    public class NotificationImageView : ImageView
    {
        private Context ctx;
        private int bubbleCount = 5;
        private Drawable originalDrawable;

        public NotificationImageView (Context context) : base (context)
        {
            ctx = context;
            originalDrawable = Drawable;
        }

        public NotificationImageView (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            ctx = context;
            originalDrawable = Drawable;
        }

        public NotificationImageView (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            ctx = context;
            originalDrawable = Drawable;
        }


        public int BubbleCount
        {
            get {
                return bubbleCount;
            } set {
                bubbleCount = value;
                UpdateBubble();
            }
        }

        public void UpdateBubble()
        {
            var currentDrawable = originalDrawable;

            var iconBitmap = ((BitmapDrawable)currentDrawable).Bitmap;
            iconBitmap = iconBitmap.Copy (Bitmap.Config.Argb8888, true);

            //Make room for bubble.
            var biggerBitmap = Bitmap.CreateBitmap (iconBitmap.Width + 5, iconBitmap.Height + 10, Bitmap.Config.Argb8888);
            var canvas = new Canvas (biggerBitmap);
            canvas.DrawBitmap (iconBitmap, 0, 10, null);
            var dm = ctx.Resources.DisplayMetrics;

            TextView bubble = new TextView (ctx);
            bubble.Text = bubbleCount.ToString();
            bubble.TextSize= (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 4, dm);;
            bubble.Gravity = GravityFlags.Center;
            bubble.SetTextColor (ctx.Resources.GetColor (Resource.Color.dark_gray_text));
            var bubbleBackground = ctx.Resources.GetDrawable (Resource.Drawable.NotificationBubble);
            bubble.SetBackgroundDrawable (bubbleBackground);
            bubble.SetPadding (
                (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 3, dm),
                0,
                (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 3, dm),
                0
            );

            bubble.DrawingCacheEnabled = true;
            bubble.Measure (
                MeasureSpec.MakeMeasureSpec (0, MeasureSpecMode.Unspecified),
                MeasureSpec.MakeMeasureSpec (0, MeasureSpecMode.Unspecified)
            );
            bubble.Layout (0, 0, bubble.MeasuredWidth, bubble.MeasuredHeight);
            bubble.BuildDrawingCache();
            canvas.DrawBitmap (bubble.DrawingCache, biggerBitmap.Width - bubble.Width, 0, null);
            var drawable = new BitmapDrawable (ctx.Resources, biggerBitmap);
            SetImageDrawable (drawable);
        }

    }
}

