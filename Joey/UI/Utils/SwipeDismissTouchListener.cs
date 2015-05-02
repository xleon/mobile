using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Utils
{
    public class SwipeDismissTouchListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
    {
        public interface IDismissCallbacks
        {
            bool CanDismiss (RecyclerView recyclerView, int position);

            void OnDismiss (RecyclerView recyclerView, int position);
        }

        private int slop;

        // Fixed properties
        private readonly IDismissCallbacks callbacks;
        private readonly RecyclerView recyclerView;
        private View downView;

        // Transient properties
        private float downX;
        private float downY;
        private bool swiping;
        private int swipingSlop;
        private int downPosition;
        private int minFlingVelocity;
        private int maxFlingVelocity;
        private bool IsScrolling;
        private VelocityTracker velocityTracker;

        public SwipeDismissTouchListener (RecyclerView recyclerView, IDismissCallbacks callbacks)
        {
            var viewConfiguration = ViewConfiguration.Get (recyclerView.Context);
            minFlingVelocity = viewConfiguration.ScaledMinimumFlingVelocity * 16;
            maxFlingVelocity = viewConfiguration.ScaledMaximumFlingVelocity;
            swipingSlop = viewConfiguration.ScaledTouchSlop;
            this.recyclerView = recyclerView;
            recyclerView.SetOnScrollListener ( new RecyclerViewScrollDetector (this));
            this.callbacks = callbacks;
            IsScrolling = false;
        }

        public bool IsEnabled
        {
            get {
                return ! (recyclerView.IsInLayout || IsScrolling);
            }
        }

        public bool OnInterceptTouchEvent (RecyclerView rv, MotionEvent e)
        {
            return OnTouch (rv, e);
        }

        public void OnTouchEvent (RecyclerView rv, MotionEvent e)
        {
            OnTouch (rv, e); ;
        }

        public bool OnTouch (View v, MotionEvent motionEvent)
        {
            switch (motionEvent.Action) {
            case MotionEventActions.Down:
                if (!IsEnabled) {
                    return false;
                }

                var itemRect = new Rect();
                var listViewCoords = new int[2];
                recyclerView.GetLocationOnScreen (listViewCoords);

                int x = (int) motionEvent.RawX - listViewCoords[0];
                int y = (int) motionEvent.RawY - listViewCoords[1];

                View item;
                for (int i = 0; i < recyclerView.ChildCount; i++) {
                    item = recyclerView.GetChildAt (i);
                    item.GetHitRect (itemRect);
                    if (itemRect.Contains (x, y)) {
                        downView = item;
                        break;
                    }
                }

                if (downView != null) {
                    downX = motionEvent.RawX;
                    downY = motionEvent.RawY;
                    downPosition = recyclerView.GetChildPosition (downView);
                    if (callbacks.CanDismiss (recyclerView, downPosition)) {
                        velocityTracker = VelocityTracker.Obtain ();
                        velocityTracker.AddMovement (motionEvent);
                    } else {
                        downView = null;
                    }
                }
                return false;

            case MotionEventActions.Up:

                if (downView == null || velocityTracker == null) {
                    return false;
                }

                velocityTracker.ComputeCurrentVelocity (1000);
                float velocityX = velocityTracker.XVelocity;
                float absVelocityX = Math.Abs (velocityX);
                float absVelocityY = Math.Abs (velocityTracker.YVelocity);
                if (Math.Abs (downView.ScrollX) > (downView.Width / 2) ||
                        minFlingVelocity <= absVelocityX && absVelocityX <= maxFlingVelocity && absVelocityY < absVelocityX) {
                    var swipeView = (ListItemSwipeable)downView;
                    swipeView.SlideAnimation (ListItemSwipeable.SwipeAction.Delete);
                    swipeView.SwipeAnimationEnd += OnItemDismissed;
                } else if (absVelocityX > 10 || Math.Abs (downView.ScrollX) > 1) {
                    var swipeView = (ListItemSwipeable)downView;
                    swipeView.SlideAnimation (ListItemSwipeable.SwipeAction.Cancel);
                }
                downX = 0;
                downY = 0;
                swiping = false;
                velocityTracker.Recycle();
                velocityTracker = null;
                break;

            case MotionEventActions.Move:
                if (downView == null || velocityTracker == null || !IsEnabled) {
                    return false;
                }

                velocityTracker.AddMovement (motionEvent);
                float deltaX = motionEvent.RawX - downX;
                float deltaY = motionEvent.RawY - downY;
                if (Math.Abs (deltaX) > swipingSlop && Math.Abs (deltaY) < Math.Abs (deltaX) / 2) {
                    swiping = true;
                    slop = (deltaX > 0 ? swipingSlop : -swipingSlop);
                    recyclerView.RequestDisallowInterceptTouchEvent (true);
                    MotionEvent cancelEvent = MotionEvent.Obtain (motionEvent);
                    cancelEvent.Action = MotionEventActions.Cancel;

                    recyclerView.OnTouchEvent (cancelEvent);
                    cancelEvent.Recycle();
                }

                if (swiping) {
                    if (deltaX < 0) {
                        return false;
                    }

                    downView.OverScrollMode = OverScrollMode.IfContentScrolls;
                    downView.ScrollX = (int)-deltaX + slop;

                    var swipeView = (LogTimeEntryItem)downView;
                    swipeView.OnScrollEvent ((int)deltaX - swipingSlop);
                    return true;
                }
                break;

            }
            return false;
        }

        private void OnItemDismissed (object sender, EventArgs e)
        {
            if (downPosition != -1) {
                callbacks.OnDismiss (recyclerView, downPosition);
            }
            downPosition = AdapterView.InvalidPosition;
            downView = null;
        }

        private class RecyclerViewScrollDetector : RecyclerView.OnScrollListener
        {
            private readonly SwipeDismissTouchListener touchListener;

            public RecyclerViewScrollDetector (SwipeDismissTouchListener touchListener)
            {
                this.touchListener = touchListener;
            }

            public override void OnScrollStateChanged (RecyclerView recyclerView, int newState)
            {
                base.OnScrollStateChanged (recyclerView, newState);
                touchListener.IsScrolling = (newState == RecyclerView.ScrollStateDragging);
            }
        }
    }
}