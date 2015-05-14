using System;
using Android.Animation;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Toggl.Joey.UI.Views
{
    public class ListItemSwipeable : ViewGroup
    {
        public static int DeleteLine = 120;
        protected TextView DeleteTextDialog;
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
            DeleteTextDialog.Left = -x;
        }

        public void InitSwipeDeleteBg ()
        {
            OnScrollEvent (0);
            ScrollX = 0;
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            LayoutView (DeleteTextDialog, PaddingLeft, PaddingTop, DeleteTextDialog.MeasuredWidth, DeleteTextDialog.MeasuredHeight);
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

            DeleteTextDialog = new TextView (ctx);
            DeleteTextDialog.LayoutParameters = mlp;
            DeleteTextDialog.Text = ctx.Resources.GetString (Resource.String.SwipeDeleteQuestion);
            DeleteTextDialog.Gravity = GravityFlags.CenterVertical;
            DeleteTextDialog.SetBackgroundColor (Resources.GetColor (Resource.Color.background_delete_item));
            DeleteTextDialog.SetPadding ((int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 35, displayMetrics), 0, 0, 0);
            DeleteTextDialog.SetTextColor (Android.Graphics.Color.White);

            AddView (DeleteTextDialog);
        }

        public void SlideAnimation (SwipeAction a)
        {
            var anim = ValueAnimator.OfFloat (ScrollX, a == SwipeAction.Delete ? -Width : 0);
            anim.Update += OnScrollAnimationUpdate;
            if (a == SwipeAction.Delete) {
                anim.AnimationEnd += OnScrollAnimationEnd;
            }
            anim.SetDuration (250)
            .Start ();
        }

        public event EventHandler SwipeAnimationEnd;

        private void OnScrollAnimationEnd (object sender, EventArgs e)
        {
            if (WindowVisibility == ViewStates.Gone) {
                InitSwipeDeleteBg ();
                return;
            }
            if (SwipeAnimationEnd != null) {
                SwipeAnimationEnd (this, EventArgs.Empty);
            }
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