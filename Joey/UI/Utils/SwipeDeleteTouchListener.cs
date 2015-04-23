using System;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Utils
{
    public class SwipeDeleteTouchListener : Java.Lang.Object, View.IOnTouchListener
    {
        private ListView listView;
        private IDismissCallbacks callbacks;
        private View downView;
        private float downX;
        private float downY;
        private bool swiping;
        private int swipingSlop;
        private int downPosition;
        private int minFlingVelocity;
        private int maxFlingVelocity;
        private VelocityTracker velocityTracker;

        public SwipeDeleteTouchListener (ListView listView, IDismissCallbacks callbacks)
        {
            var viewConfiguration = ViewConfiguration.Get (listView.Context);
            minFlingVelocity = viewConfiguration.ScaledMinimumFlingVelocity * 16;
            maxFlingVelocity = viewConfiguration.ScaledMaximumFlingVelocity;
            swipingSlop = viewConfiguration.ScaledTouchSlop;
            this.listView = listView;
            this.callbacks = callbacks;
        }

        public bool OnTouch (View view, MotionEvent motionEvent)
        {
            switch (motionEvent.Action) {
            case MotionEventActions.Down:

                var itemRect = new Rect();
                var listViewCoords = new int[2];
                listView.GetLocationOnScreen (listViewCoords);

                int x = (int) motionEvent.RawX - listViewCoords[0];
                int y = (int) motionEvent.RawY - listViewCoords[1];

                View listItem;
                for (int i = 0; i < listView.ChildCount; i++) {
                    listItem = listView.GetChildAt (i);
                    listItem.GetHitRect (itemRect);

                    if (itemRect.Contains (x, y)) {
                        downView = listItem;
                        break;
                    }
                }

                if (downView != null) {

                    downX = motionEvent.RawX;
                    downY = motionEvent.RawY;
                    downPosition = listView.GetPositionForView (downView);
                    if (callbacks.CanDismiss (downPosition)) {
                        velocityTracker = VelocityTracker.Obtain ();
                        velocityTracker.AddMovement (motionEvent);
                    } else {
                        downView = null;
                    }
                }
                return false;

            case MotionEventActions.Up:
                if (downView == null) {
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
                if (downView == null) {
                    return false;
                }

                velocityTracker.AddMovement (motionEvent);
                float deltaX = motionEvent.RawX - downX;
                float deltaY = motionEvent.RawY - downY;
                if (Math.Abs (deltaX) > swipingSlop && Math.Abs (deltaY) < Math.Abs (deltaX) / 2) {
                    swiping = true;
                    listView.RequestDisallowInterceptTouchEvent (true);
                    MotionEvent cancelEvent = MotionEvent.Obtain (motionEvent);

                    listView.OnTouchEvent (cancelEvent);
                    cancelEvent.Recycle();
                }

                if (swiping) {
                    if (deltaX < 0) {
                        return false;
                    }

                    downView.OverScrollMode = OverScrollMode.IfContentScrolls;
                    downView.ScrollX = (int)-deltaX + swipingSlop;

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
                callbacks.OnDismiss (downPosition);
            }
            downPosition = AdapterView.InvalidPosition;
            downView = null;
        }

        public interface IDismissCallbacks
        {
            bool CanDismiss (int position);

            void OnDismiss (int position);
        }
    }
}