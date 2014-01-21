using System;
using System.Linq;
using Android.App;
using Android.OS;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true)]
    public class RecentTimeEntriesActivity : BaseActivity
    {
        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);
        }
    }
}
