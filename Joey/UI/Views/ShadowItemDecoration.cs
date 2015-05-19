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
        private const int shadowHeightInDps = 2;
        private readonly int shadowHeightInPixels;

        private readonly Drawable shadow;
        private readonly bool detectHolder;

        public ShadowItemDecoration (Context context, bool detectHolder = false)
        {
            shadow = context.Resources.GetDrawable (Resource.Drawable.DropShadowVertical);
            shadowHeightInPixels = (int) (context.Resources.DisplayMetrics.Density * shadowHeightInDps + 0.5f);
            this.detectHolder = detectHolder;
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

                var childType = detectHolder ? GetHolderType (parent, child) : child.GetType ();
                var childNextType = detectHolder ? GetHolderType (parent, childNext) : childNext.GetType();


                if (!ShouldDraw (child) || childNextType == typeof (T)) {
                    continue;
                }

                if (childType == typeof (T)) {
                    var layoutParams = child.LayoutParameters.JavaCast<RecyclerView.LayoutParams> ();
                    var top = child.Bottom + layoutParams.BottomMargin;

                    if (shadowHeightInPixels > 0) {
                        var bottom = top + shadowHeightInPixels;
                        shadow.SetBounds (parent.PaddingLeft, top, child.Right+child.PaddingRight, bottom);
                        shadow.Draw (c);
                    }
                }
            }

        }

        private Type GetHolderType (RecyclerView parent, View child)
        {
            return parent.GetChildViewHolder (child).GetType ();
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

