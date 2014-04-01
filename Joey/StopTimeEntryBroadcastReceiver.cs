using System;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Joey
{
    [BroadcastReceiver (Exported = true)]
    class StopTimeEntryBroadcastReceiver: BroadcastReceiver
    {
        private const string LogTag = "StopTimeEntryBroadcastReceiver";

        public override void OnReceive (Context context, Intent intent)
        {
            TimeEntryModel timeEntry = TimeEntryModel.FindRunning ();
            if (timeEntry != null) {
                timeEntry.Stop ();
            }

            // Force commit of data (in case Android kills us right after this function returns)
            var modelStore = ServiceContainer.Resolve<IModelStore> ();
            try {
                modelStore.Commit ();
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Warning (LogTag, ex, "Manual commit failed.");
            }

            // Try initialising components
            var app = context.ApplicationContext as AndroidApp;
            if (app != null) {
                app.InitializeComponents ();
            }
        }
    }
}

