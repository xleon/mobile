using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Joey.UI.Utils;

namespace Toggl.Joey.UI.Views
{
    public class ShadowItemDecoration : RecyclerView.ItemDecoration
    {
        private const int topShadowHeightInDps = 2;
        private const int bottomShadowHeightInDps = 1;

        private readonly int topShadowHeightInPixels;
        private readonly int bottomShadowHeightInPixels;

        private readonly Drawable shadow, reverseShadow;

        private const ShadowAttribute.Mode bottom = ShadowAttribute.Mode.Bottom;
        private const ShadowAttribute.Mode top = ShadowAttribute.Mode.Top;

        public ShadowItemDecoration (Context context)
        {
            shadow = context.Resources.GetDrawable (Resource.Drawable.DropShadowVertical);
            reverseShadow = context.Resources.GetDrawable (Resource.Drawable.DropShadowVerticalReverse);

            topShadowHeightInPixels = topShadowHeightInDps.DpsToPxls (context);
            bottomShadowHeightInPixels = bottomShadowHeightInDps.DpsToPxls (context);
        }

        public override void OnDraw (Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            var childCount = parent.ChildCount;

            if (childCount == 0) {
                return;
            }

            for (int i = 0; i+1 < childCount; i++) {
                var child = parent.GetChildAt (i);
                var viewHolder = parent.GetChildViewHolder (child);

                Type type = viewHolder.GetType ();
                object[] attributes = type.GetCustomAttributes (typeof (ShadowAttribute), false);

                if (attributes.Length != 1) {
                    continue;
                }

                ShadowAttribute shadowAttr = attributes [0] as ShadowAttribute;

                if (ShouldDraw (child) && shadowAttr != null) {

                    var m = shadowAttr.Modes;
                    var left = parent.PaddingLeft;
                    var right = child.Right + child.PaddingRight;

                    if (m.HasFlag (top) && topShadowHeightInPixels > 0 && parent.GetChildAdapterPosition (child) != 0) {
                        var shadowBottom = child.Top + topShadowHeightInPixels;
                        shadow.SetBounds (left, child.Top, right, shadowBottom);
                        shadow.Draw (c);
                    }

                    if (m.HasFlag (bottom) && bottomShadowHeightInPixels > 0) {
                        var reverseShadowTop = child.Bottom - bottomShadowHeightInPixels;
                        reverseShadow.SetBounds (left, reverseShadowTop, right, child.Bottom);
                        reverseShadow.Draw (c);
                    }
                }
            }
        }

        private bool ShouldDraw (View child)
        {
            if (child.Visibility != ViewStates.Visible || child.Alpha != 1.0f) {
                return false;
            }
            return true;
        }
    }

    [AttributeUsage (AttributeTargets.Class)]
    public class ShadowAttribute : Attribute
    {
        [Flags]
        public enum Mode { Top = 1, Bottom = 2 };
        public readonly Mode Modes;

        public ShadowAttribute (Mode modes)
        {
            Modes = modes;
        }
    }
}

