
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class FeedbackFragment : Fragment
    {
        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.FeedbackFragment, container, false);
            return view;
        }

    }
}

