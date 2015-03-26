using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics.Drawables;
using System;
using Android.Animation;

namespace Toggl.Joey.UI.Views
{
    public class ListItemSwipeable : ViewGroup
    {
        public static int DeleteLine = 120;
        protected TextView deleteTextDialog;
        private Context ctx;

        public ListItemSwipeable (Context context) : base (context)
        {
            ctx = context;
            ImplementDelete();
        }

        public ListItemSwipeable (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            ctx = context;
            ImplementDelete();
        }

        public ListItemSwipeable (Context context, IAttributeSet attrs, int defStyle): base (context, attrs, defStyle)
        {
            ctx = context;
            ImplementDelete();
        }

        public void OnScrollEvent (int x)
        {
            deleteTextDialog.Left = -x;
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            LayoutView (deleteTextDialog, PaddingLeft, PaddingTop, deleteTextDialog.MeasuredWidth, deleteTextDialog.MeasuredHeight);
        }

        protected void LayoutView (View view, int left, int top, int width, int height)
        {
            var margins = (MarginLayoutParams)view.LayoutParameters;
            int leftWithMargins = left + margins.LeftMargin;
            int topWithMargins = top + margins.TopMargin;

            view.Layout (leftWithMargins, topWithMargins,
                         leftWithMargins + width, topWithMargins + height);
        }

        private void ImplementDelete()
        {
            var displayMetrics = ctx.Resources.DisplayMetrics;
            int height = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 72, displayMetrics);
            var mlp = new ViewGroup.MarginLayoutParams (LayoutParams.MatchParent, height);
            deleteTextDialog = new TextView (ctx);
            deleteTextDialog.LayoutParameters = mlp;
            deleteTextDialog.Text = ctx.Resources.GetString (Resource.String.SwipeDeleteQuestion);
            deleteTextDialog.Gravity = GravityFlags.CenterVertical;
            deleteTextDialog.SetBackgroundColor (Android.Graphics.Color.ParseColor ("#333333"));
            deleteTextDialog.SetPadding ((int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 35, displayMetrics), 0, 0, 0);
            deleteTextDialog.SetTextColor (Android.Graphics.Color.White);
            this.AddView (deleteTextDialog);
        }

        public void SlideAnimation (SwipeAction a)
        {
            var anim = ValueAnimator.OfFloat (ScrollX, a == SwipeAction.Delete ? -Width : 0);
            anim.Update += OnScrollAnimationUpdate;
            anim.SetDuration (250);
            anim.Start ();
        }

        private void OnScrollAnimationUpdate (object sender, ValueAnimator.AnimatorUpdateEventArgs e)
        {
            ScrollX = (int)e.Animation.AnimatedValue;
            OnScrollEvent (-ScrollX);
        }

        public enum SwipeAction {
            Delete,
            Cancel
        }
    }
}