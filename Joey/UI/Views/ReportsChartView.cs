using Android.Content;
using Android.Util;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public class ReportsChartView : ViewGroup
    {
        public ReportsChartView (Context context) :
            base (context)
        {
            Initialize ();
        }

        public ReportsChartView (Context context, IAttributeSet attrs) :
            base (context, attrs)
        {
            Initialize ();
        }

        public ReportsChartView (Context context, IAttributeSet attrs, int defStyle) :
            base (context, attrs, defStyle)
        {
            Initialize ();
        }

        void Initialize ()
        {
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
        }

    }
}

