using System;
using Android.Content;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey
{
    [BroadcastReceiver (Exported=true)]
    class StopTimeEntryBroadcastReceiver: BroadcastReceiver
    {
        public override void OnReceive (Context context, Intent intent)
        {
            TimeEntryModel timeEntry = TimeEntryModel.FindRunning();
            if (timeEntry != null) {
                timeEntry.Stop ();
            }
        }
    }
}

