using System;
using Android.App;
using Android.Content;
using Android.Support.V4.Content;

namespace Toggl.Joey.Net
{
    [BroadcastReceiver (Permission = "com.google.android.c2dm.permission.SEND")]
    [IntentFilter (new string[] { "com.google.android.c2dm.intent.RECEIVE" },
                   Categories = new string[] { "com.toggl.timer" })]
    public class GcmBroadcastReceiver : WakefulBroadcastReceiver
    {
        public override void OnReceive (Context context, Intent intent)
        {
            var serviceIntent = new Intent (context, typeof(GcmService));
            serviceIntent.ReplaceExtras (intent.Extras);
            StartWakefulService (context, serviceIntent);

            ResultCode = Result.Ok;
        }
    }
}
