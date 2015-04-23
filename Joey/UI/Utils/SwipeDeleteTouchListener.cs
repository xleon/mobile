using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Utils
{
    public class SwipeDeleteTouchListener : Java.Lang.Object, View.IOnTouchListener
    {
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
        private bool isEnabled;
        private VelocityTracker velocityTracker;

        public SwipeDeleteTouchListener (RecyclerView recyclerView, IDismissCallbacks callbacks)
        {
            var viewConfiguration = ViewConfiguration.Get (recyclerView.Context);
            minFlingVelocity = viewConfiguration.ScaledMinimumFlingVelocity * 16;
            maxFlingVelocity = viewConfiguration.ScaledMaximumFlingVelocity;
            swipingSlop = viewConfiguration.ScaledTouchSlop;
            this.recyclerView = recyclerView;
            this.callbacks = callbacks;
        }

        public bool IsEnabled
        {
            get {
                return isEnabled;
            } set {
                if (value == IsEnabled) {
                    return;
                }
                isEnabled = value;
            }
        }

        public bool OnTouch (View view, MotionEvent motionEvent)
        {
            switch (motionEvent.Action) {
            case MotionEventActions.Down:
                if (!isEnabled) {
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
                    if (callbacks.CanDismiss (downPosition)) {
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
                        minFlingVelocity <= absVelocityX && absVelocityX <= maxFlingVelocity && absVelocityY < absVelocityX ) {
                    var swipeView = (ListItemSwipeable)downView;
                    swipeView.SlideAnimation (ListItemSwipeable.SwipeAction.Delete);
                    swipeView.SwipeAnimationEnd += DeleteItem;
                } else {
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
                if (downView == null || velocityTracker == null || !isEnabled) {
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

        private void DeleteItem (object sender, EventArgs e)
        {
            if (downPosition != -1) {
                callbacks.OnDismiss (recyclerView, downPosition);
            }
            downPosition = AdapterView.InvalidPosition;
            downView = null;
        }

        public interface IDismissCallbacks
        {
            bool CanDismiss (int position);

            void OnDismiss (RecyclerView recyclerView, int position);
        }
    }
}