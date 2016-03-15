using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public class DividerItemDecoration : RecyclerView.ItemDecoration
    {
        private int[] Attrs = { Android.Resource.Attribute.ListDivider };
        private Drawable divider;
        private int orientation;

        public const int HorizontalList = LinearLayoutManager.Horizontal;
        public const int VerticalList = LinearLayoutManager.Vertical;

        // Explanation of native constructor
        // http://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview/10603714#10603714
        public DividerItemDecoration (IntPtr a, JniHandleOwnership b) : base (a, b)
        {
        }

        public DividerItemDecoration (Context context, int orientation)
        {
            var a = context.ObtainStyledAttributes (Attrs);
            divider = a.GetDrawable (0);
            a.Recycle();
            Orientation = orientation;
        }

        /// <summary>
        /// Gets or sets orientation
        /// </summary>
        public int Orientation
        {
            get { return orientation; }
            set {
                if (value != HorizontalList && value != VerticalList) {
                    throw new ArgumentException ("Invalid orientation", "value");
                }
                orientation = value;
            }
        }

        public void DrawVertical (Canvas c, RecyclerView parent)
        {
            var left = parent.PaddingLeft;
            var right = parent.PaddingRight;

            var childCount = parent.ChildCount;
            for (int i = 0; i < childCount; i++) {
                using (View child = parent.GetChildAt (i)) {
                    var top = child.Bottom;
                    var bottom = top + divider.IntrinsicHeight;
                    divider.SetBounds (left, top, right, bottom);
                    divider.Draw (c);
                }
            }
        }

        public void DrawHorizontal (Canvas c, RecyclerView parent)
        {
            var top = parent.PaddingTop;
            var bottom = parent.PaddingBottom;
            RecyclerView.LayoutParams layoutParams = null;
            View child = null;

            var childCount = parent.ChildCount;
            for (int i = 0; i < childCount; i++) {
                child = parent.GetChildAt (i);
                layoutParams = child.LayoutParameters.JavaCast<RecyclerView.LayoutParams>();
                var left = child.Right + layoutParams.RightMargin;
                var right = left + divider.IntrinsicHeight;
                divider.SetBounds (left, top, right, bottom);
                divider.Draw (c);
            }

            if (child != null) {
                child.Dispose ();
            }
            if (layoutParams != null) {
                layoutParams.Dispose ();
            }
        }

        public override void GetItemOffsets (Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {
            if (orientation == VerticalList) {
                outRect.Set (0, 0, 0, divider.IntrinsicHeight);
            } else {
                outRect.Set (0, 0, divider.IntrinsicWidth, 0);
            }
        }

        public override void OnDraw (Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            if (orientation == VerticalList) {
                DrawVertical (c, parent);
            } else {
                DrawHorizontal (c, parent);
            }
        }
    }
}