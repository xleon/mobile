using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Toggl.Joey.UI.Views
{
    public class NoSwipePager : ViewPager
    {
        private bool enableSwipe = false;

        public NoSwipePager (Context context) : base (context)
        {
        }

        public NoSwipePager (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public bool EnableSwipe
        {
            get {
                return enableSwipe;
            } set {
                enableSwipe = value;
            }
        }
        public override bool OnTouchEvent (MotionEvent e)
        {
            if (enableSwipe) {
                base.OnTouchEvent (e);
            }
            return false;
        }

        public override bool OnInterceptTouchEvent (MotionEvent ev)
        {
            if (enableSwipe) {
                return base.OnInterceptTouchEvent (ev);
            }
            return false;
        }
    }
}
