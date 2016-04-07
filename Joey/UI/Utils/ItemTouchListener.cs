using System;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Java.Lang;

namespace Toggl.Joey.UI.Utils
{
    public class ItemTouchListener : GestureDetector.SimpleOnGestureListener, RecyclerView.IOnItemTouchListener
    {
        public interface IItemTouchListener
        {
            void OnItemClick(RecyclerView parent, View clickedView, int position);

            void OnItemLongClick(RecyclerView parent, View clickedView, int position);

            bool CanClick(RecyclerView parent, int position);
        }

        private IItemTouchListener listener;
        private readonly RecyclerView recyclerView;
        private GestureDetector gestureDetector;
        private bool IsScrolling;

        // Explanation of native constructor
        // http://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview/10603714#10603714
        public ItemTouchListener(IntPtr a, JniHandleOwnership b) : base(a, b)
        {
        }

        public ItemTouchListener(RecyclerView recyclerView, IItemTouchListener listener)
        {

            if (recyclerView == null || listener == null)
            {
                throw new IllegalArgumentException("RecyclerView and Listener arguments can not be null");
            }

            IsScrolling = false;
            this.recyclerView = recyclerView;
            this.listener = listener;
            gestureDetector = new GestureDetector(recyclerView.Context, this);
            recyclerView.AddOnScrollListener(new RecyclerViewScrollDetector(this));
        }

        private bool IsEnabled
        {
            get
            {
                var isEnabled = !recyclerView.GetItemAnimator().IsRunning || IsScrolling;
                if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.JellyBeanMr1)
                {
                    isEnabled = !(recyclerView.IsInLayout || recyclerView.GetItemAnimator().IsRunning || IsScrolling);
                }
                return isEnabled;
            }
        }

        public bool OnInterceptTouchEvent(RecyclerView rv, MotionEvent e)
        {
            // TODO : this part intercep any touch inside recycler
            // and delete pending items.
            // A better method could be used.

            if (e.Action == MotionEventActions.Down)
            {
                var undoAdapter = (IUndoAdapter)rv.GetAdapter();
                View view = GetChildViewUnder(e);

                if (view == null)
                {
                    undoAdapter.DeleteSelectedItem();
                }
                else
                {
                    int position = recyclerView.GetChildLayoutPosition(view);
                    if (!undoAdapter.IsUndo(position))
                    {
                        undoAdapter.DeleteSelectedItem();
                    }
                }
            }

            if (IsEnabled)
            {
                gestureDetector.OnTouchEvent(e);
            }
            return false;
        }

        public void OnTouchEvent(RecyclerView rv, MotionEvent e)
        {
        }

        public override void OnShowPress(MotionEvent e)
        {
            View view = GetChildViewUnder(e);
            if (view != null)
            {
                view.Pressed = true;
                view.Selected = true;
            }
        }

        public override void OnLongPress(MotionEvent e)
        {
            OnSingleTapUp(e);
        }

        public override bool OnSingleTapUp(MotionEvent e)
        {
            View view = GetChildViewUnder(e);
            if (view == null) { return false; }

            int position = recyclerView.GetChildLayoutPosition(view);
            if (listener.CanClick(recyclerView, position))
            {
                var undoAdapter = (IUndoAdapter)recyclerView.GetAdapter();
                if (!undoAdapter.IsUndo(position))
                {
                    listener.OnItemClick(recyclerView, view, position);
                    view.Pressed = false;
                    view.Selected = false;
                    return true;
                }
            }
            return false;
        }

        public void OnRequestDisallowInterceptTouchEvent(bool p0)
        {
        }

        private View GetChildViewUnder(MotionEvent e)
        {
            var view = recyclerView.FindChildViewUnder(e.GetX(), e.GetY());
            if (view == null)
            {
                return null;
            }

            // Find better way!
            var playBtn = view.FindViewById<Android.Widget.ImageButton> (Resource.Id.ContinueImageButton);
            if (playBtn != null)
            {
                return IsPointInsideView(e.GetX(), e.GetY(), playBtn) ? null : view;
            }
            return view;
        }

        private bool IsPointInsideView(float x, float y, View view)
        {
            var location = new int[2];
            view.GetLocationOnScreen(location);
            int viewX = location [0];

            //point is inside view bounds
            if (x > viewX && x < (viewX + view.Width))
            {
                return true;
            }

            return false;
        }

        private class RecyclerViewScrollDetector : RecyclerView.OnScrollListener
        {
            private readonly ItemTouchListener touchListener;

            public RecyclerViewScrollDetector(ItemTouchListener touchListener)
            {
                this.touchListener = touchListener;
            }

            public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
            {
                base.OnScrollStateChanged(recyclerView, newState);
                touchListener.IsScrolling = newState != RecyclerView.ScrollStateIdle;
            }
        }
    }
}

