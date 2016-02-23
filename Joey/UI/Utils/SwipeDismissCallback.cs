using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Toggl.Joey.UI.Adapters;

namespace Toggl.Joey.UI.Utils
{
    public class SwipeDismissCallback : ItemTouchHelper.SimpleCallback
    {
        public interface IDismissListener
        {
            bool CanDismiss (RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder);
        }

        private const float minThreshold = 20;
        private IDismissListener listener;

        public SwipeDismissCallback (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        public SwipeDismissCallback (int p0, int p1, IDismissListener listener) : base (p0, p1)
        {
            this.listener = listener;
        }

        public override bool OnMove (RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, RecyclerView.ViewHolder target)
        {
            return false;
        }

        public override int GetSwipeDirs (RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            if (listener.CanDismiss (recyclerView, viewHolder)) {
                return ItemTouchHelper.Right;
            }
            return 0;
        }

        public override void OnSwiped (RecyclerView.ViewHolder viewHolder, int direction)
        {
            var adapter = (IUndoAdapter)recycler.GetAdapter ();
            adapter.SetItemToUndoPosition (viewHolder);
        }

        public override float GetSwipeThreshold (RecyclerView.ViewHolder viewHolder)
        {
            return minThreshold;
        }

        private RecyclerView recycler;

        public override void OnChildDraw (Canvas cValue, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
        {
            if (viewHolder != null) {
                var view = viewHolder.ItemView.FindViewById (Resource.Id.swipe_layout);
                DefaultUIUtil.OnDraw (cValue, recyclerView, view, dX, dY, actionState, isCurrentlyActive);
            }
            recycler = recyclerView;
        }

        public override void ClearView (RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            // Let ClearView clean and don't
            // cal; native ClearView. This operation
            // will be done inside ViewHolder.
        }
    }
}

