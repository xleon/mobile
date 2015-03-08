using System;
using Android.Views;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Content;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace Toggl.Joey.UI.Decorations
{
    public class ItemDividerDecoration : RecyclerView.ItemDecoration
    {
        private readonly Drawable divider;

        public ItemDividerDecoration (Context context)
        {
            divider = context.Resources.GetDrawable (Resource.Drawable.LineDivider);
        }

        public override void OnDrawOver (Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            var left = parent.PaddingLeft;
            var right = parent.Width - parent.PaddingRight;

            var childCount = parent.ChildCount;

            for (int i = 0; i < childCount; i++) {
                var child = parent.GetChildAt (i);

                var top = child.Bottom;
                int bottom = top + divider.IntrinsicHeight;

                divider.SetBounds (left, top, right, bottom);
                divider.Draw (c);
            }
        }
    }
}

