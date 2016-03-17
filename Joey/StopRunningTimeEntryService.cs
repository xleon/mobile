using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Support.V4.Content;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey
{
    [Service (Exported = false)]
    public sealed class StopRunningTimeEntryService : Service
    {
        private static readonly string Tag = "StopRunningTimeEntryService";

        public StopRunningTimeEntryService () : base ()
        {
        }

        public StopRunningTimeEntryService (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base (javaRef, transfer)
        {
        }

        public override async void OnStart (Intent intent, int startId)
        {
            try {
                var stopTask = FindAndStopRunning ();

                // Try initialising components (while the changes are being made)
                var app = Application as AndroidApp;
                if (app != null) {
                    app.InitializeComponents ();
                }

                // Wait until the changes have been commited to the database before stopping the service
                await stopTask;
            } finally {
                Receiver.CompleteWakefulIntent (intent);
                StopSelf (startId);
            }
        }

        private static async Task FindAndStopRunning ()
        {
            await Task.Delay (1);
            /*
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
            var dataStore = ServiceContainer.Resolve<IDataStore> ();

            // Find running tasks:
            var runningEntries = await dataStore.Table<TimeEntryData> ()
                                 .Where (r => r.State == TimeEntryState.Running && r.DeletedAt == null && r.UserId == userId)
                                 .ToListAsync ()
                                 .ConfigureAwait (false);

            var stopTasks = runningEntries.Select (data => TimeEntryModel.StopAsync (data));
            await Task.WhenAll (stopTasks).ConfigureAwait (false);
            */
            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.Notification);
        }

        public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
        {
            OnStart (intent, startId);

            return StartCommandResult.Sticky;
        }

        public override Android.OS.IBinder OnBind (Intent intent)
        {
            return null;
        }

        [BroadcastReceiver (Exported = true)]
        public sealed class Receiver : WakefulBroadcastReceiver
        {
            public override void OnReceive (Context context, Intent intent)
            {
                var serviceIntent = new Intent (context, typeof (StopRunningTimeEntryService));
                StartWakefulService (context, serviceIntent);
            }
        }
    }
}
