using System;
using Android.App;
using Android.Content;
using Android.Support.V4.Content;

namespace Toggl.Joey.Net
{
    [BroadcastReceiver (Permission = "com.google.android.c2dm.permission.SEND")]
    [IntentFilter (new string[] { "com.google.android.c2dm.intent.RECEIVE" },
        Categories = new string[]{ "com.toggl.android" })]
    public class GcmBroadcastReceiver : WakefulBroadcastReceiver
    {
        public override void OnReceive (Context context, Intent intent)
        {
            var comp = new ComponentName (context,
                           Java.Lang.Class.FromType (typeof(GcmService)));
            StartWakefulService (context, (intent.SetComponent (comp)));

            ResultCode = Result.Ok;
        }
    }
}
