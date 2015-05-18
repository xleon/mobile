using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public interface IShadowItemDecorationHost
    {
        Type LastItemTypeToApplyShadow { get; }
    }

    public class ShadowItemDecoration<T> : RecyclerView.ItemDecoration
    {
        private const int shadowHeight = 14;

        private readonly Drawable shadow;

        public ShadowItemDecoration (Context context)
        {
            shadow = context.Resources.GetDrawable (Resource.Drawable.DropShadowVertical);
        }

        public override void OnDraw (Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            var childCount = parent.ChildCount;

            if (childCount == 0) {
                return;
            }

            for (int i = 0; i+1 < childCount; i++) {
                var child = parent.GetChildAt (i);
                var childNext = parent.GetChildAt (i+1);

                if (!ShouldDraw (child) || childNext is T) {
                    continue;
                }

                if (child is T) {
                    var layoutParams = child.LayoutParameters.JavaCast<RecyclerView.LayoutParams> ();
                    var top = child.Bottom + layoutParams.BottomMargin;

                    var bottom = top + shadowHeight;
                    shadow.SetBounds (parent.PaddingLeft, top, child.Right+child.PaddingRight, bottom);
                    shadow.Draw (c);
                }
            }

        }

        private bool ShouldDraw (View child)
        {
            if (child.Visibility != ViewStates.Visible
                    || child.Alpha != 1.0f
                    || child.Background == null) {
                return false;
            }
            return true;

        }
    }
}

