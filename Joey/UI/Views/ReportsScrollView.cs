using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using Toggl.Joey.UI.Fragments;

namespace Toggl.Joey.UI.Views
{
    public class ReportsScrollView : ScrollView, GestureDetector.IOnGestureListener
    {
        private int topPosition;
        private int bottomPosition;
        private View barChartView;
        private View pieChartView;
        private View listChartView;
        private const float snapPadding = 30;
        private int currentSnap;

        private ChartPosition _position;

        public ChartPosition Position
        {
            get {
                return _position;
            } set {
                if (_position == value) {
                    return;
                }
                _position = value;
                currentSnap = (_position == ChartPosition.Top) ? topPosition : bottomPosition;
                ScrollTo ( Left, currentSnap);
                //ScrollY = currentSnap;
                Console.WriteLine ( "go to ; " +  currentSnap);
            }
        }

        public event EventHandler PositionChanged;

        public ReportsScrollView (Context context) :
        base (context)
        {
            Initialize (context);
        }

        public ReportsScrollView (Context context, IAttributeSet attrs) :
        base (context, attrs)
        {
            Initialize (context);
        }

        public ReportsScrollView (Context context, IAttributeSet attrs, int defStyle) :
        base (context, attrs, defStyle)
        {
            Initialize (context);
        }

        private void Initialize (Context ctx)
        {

        }

        protected override void OnAttachedToWindow ()
        {
            base.OnAttachedToWindow ();

            barChartView = FindViewById<View> (Resource.Id.BarChart);
            pieChartView = FindViewById<View> (Resource.Id.PieChart);
            listChartView = FindViewById<View> (Android.Resource.Id.List);
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure (widthMeasureSpec, heightMeasureSpec);

            if (Height > 0) {
                var layoutParams = listChartView.LayoutParameters;
                layoutParams.Height = Height - barChartView.Height;
                listChartView.LayoutParameters = layoutParams;
                listChartView.RequestLayout ();
                topPosition = 0;
                bottomPosition = barChartView.Bottom + ((ViewGroup.MarginLayoutParams)barChartView.LayoutParameters).BottomMargin;

                /*
                Console.WriteLine ( "init with " +  currentSnap);
                currentSnap = (_position == ChartPosition.Top) ? topPosition : bottomPosition;
                ScrollY = currentSnap; */
            }
        }

        #region IOnGestureListener

        public override bool OnInterceptTouchEvent (MotionEvent ev)
        {
            if (listChartView == null) {
                return base.OnInterceptTouchEvent (ev);
            };

            if ( ScrollY == bottomPosition && ev.GetY () > Convert.ToSingle (listChartView.Top)) {
                return false;
            }

            return base.OnInterceptTouchEvent (ev);
        }

        public override bool OnTouchEvent (MotionEvent e)
        {
            if (e.Action == MotionEventActions.Up) {
                currentSnap = (ScrollY > (bottomPosition - topPosition) / 2) ? bottomPosition : topPosition;
                _position = ( currentSnap == topPosition) ? ChartPosition.Top : ChartPosition.Bottom;
                if (PositionChanged != null) {
                    PositionChanged.Invoke (this, new EventArgs ());
                }
                SmoothScrollTo (Left, currentSnap);
            }
            return base.OnTouchEvent (e);
        }

        public bool OnScroll (MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            return true;
        }

        public void OnShowPress (MotionEvent e)
        {
        }

        public bool OnDown (MotionEvent e)
        {
            return false;
        }

        public bool OnFling (MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            return false;
        }

        public void OnLongPress (MotionEvent e)
        {
        }

        public bool OnSingleTapUp (MotionEvent e)
        {
            return true;
        }

        #endregion
    }
}

