using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Wearable.Views;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Toggl.Chandler.UI.Views
{
    public class RecentListItemLayout : LinearLayout, WearableListView.IOnCenterProximityListener
    {
        private TextView DescriptionTextView;
        private TextView ProjectTextView;
        private ImageView ProjectColorDot;

        private float fadedAlpha;

        public RecentListItemLayout (Context context) :
        base (context)
        {
            Initialize ();
        }

        public RecentListItemLayout (Context context, IAttributeSet attrs) :
        base (context, attrs)
        {
            Initialize ();
        }

        public RecentListItemLayout (Context context, IAttributeSet attrs, int defStyle) :
        base (context, attrs, defStyle)
        {
            Initialize ();
        }

        void Initialize ()
        {
            fadedAlpha = 50f/100f;
        }

        protected override void OnFinishInflate ()
        {
            DescriptionTextView = FindViewById<TextView> (Resource.Id.RecentListDescription);
            ProjectTextView = FindViewById<TextView> (Resource.Id.RecentListProject);
            ProjectColorDot = FindViewById<ImageView> (Resource.Id.ProjectColorDot);
            base.OnFinishInflate ();
        }

        public void OnCenterPosition (bool p0)
        {
            DescriptionTextView.Alpha = 1f;
            ProjectTextView.Alpha = 1f;
            ProjectColorDot.Alpha = 1f;
        }

        public void OnNonCenterPosition (bool p0)
        {
            DescriptionTextView.Alpha = fadedAlpha;
            ProjectTextView.Alpha = fadedAlpha;
            ProjectColorDot.Alpha = fadedAlpha;
        }
    }
}
