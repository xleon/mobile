using Android.Support.V7.Widget;
using Android.Views;
using Java.Lang;

namespace Toggl.Joey.UI.Utils
{
    public class ItemTouchListener : GestureDetector.SimpleOnGestureListener, RecyclerView.IOnItemTouchListener
    {
        public interface IItemTouchListener
        {
            void OnItemClick (RecyclerView parent, View clickedView, int position);

            void OnItemLongClick (RecyclerView parent, View clickedView, int position);
        }

        private IItemTouchListener listener;
        private readonly RecyclerView recyclerView;
        private GestureDetector gestureDetector;

        public ItemTouchListener (RecyclerView recyclerView, IItemTouchListener listener)
        {

            if (recyclerView == null || listener == null) {
                throw new IllegalArgumentException ("RecyclerView and Listener arguments can not be null");
            }

            this.recyclerView = recyclerView;
            this.listener = listener;
            gestureDetector = new GestureDetector (recyclerView.Context, this);
        }

        private bool IsEnabled
        {
            get {
                return ! (recyclerView.IsInLayout || recyclerView.GetItemAnimator().IsRunning);
            }
        }

        public bool OnInterceptTouchEvent (RecyclerView rv, MotionEvent e)
        {
            if (IsEnabled) {
                gestureDetector.OnTouchEvent (e);
            }
            return false;
        }

        public void OnTouchEvent (RecyclerView rv, MotionEvent e)
        {
        }

        public override void OnShowPress (MotionEvent e)
        {
            View view = GetChildViewUnder (e);
            if (view != null) {
                view.Pressed = true;
            }
        }

        public override void OnLongPress (MotionEvent e)
        {
            View view = GetChildViewUnder (e);
            if (view == null) { return; }

            int position = recyclerView.GetChildPosition (view);
            listener.OnItemLongClick (recyclerView, view, position);
            view.Pressed = false;
        }

        public override bool OnSingleTapUp (MotionEvent e)
        {
            View view = GetChildViewUnder (e);
            if (view == null) { return false; }

            view.Pressed = false;
            int position = recyclerView.GetChildPosition (view);
            listener.OnItemClick (recyclerView, view, position);

            return true;
        }

        private View GetChildViewUnder (MotionEvent e)
        {
            var view = recyclerView.FindChildViewUnder (e.GetX(), e.GetY());
            var playBtn = view.FindViewById<Android.Widget.ImageButton> (Resource.Id.ContinueImageButton);
            return IsPointInsideView (e.GetX(), e.GetY(), playBtn) ? null : view;
        }

        private bool IsPointInsideView (float x, float y, View view)
        {
            var location = new int[2];
            view.GetLocationOnScreen (location);
            int viewX = location [0];

            //point is inside view bounds
            if (x > viewX && x < (viewX + view.Width)) {
                return true;
            }

            return false;
        }
    }
}

