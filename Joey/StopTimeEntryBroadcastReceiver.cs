using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey
{
    [BroadcastReceiver (Exported=false)]
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

