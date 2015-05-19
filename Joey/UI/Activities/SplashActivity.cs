using Android.App;
using Android.OS;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "@string/EntryName", Theme = "@style/Theme.Toggl.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : Activity
    {
        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);
            StartActivity (typeof (MainDrawerActivity));
        }
    }
}

