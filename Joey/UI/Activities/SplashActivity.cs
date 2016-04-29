using System;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
         ScreenOrientation = ScreenOrientation.Portrait,
         Name = "toggl.joey.ui.activities.SplashActivity",
         Label = "@string/EntryName",
         Theme = "@style/Theme.Toggl.Splash",
         MainLauncher = true,
         NoHistory = true)]
    public class SplashActivity : Activity
    {
        // Explanation of native constructor
        // http://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview/10603714#10603714
        public SplashActivity(IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        public SplashActivity()
        {
        }

        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);
            StartActivity (typeof (MainDrawerActivity));
        }
    }
}

