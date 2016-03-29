using System;
using System.Linq;
using Android.App;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.Net
{
    // TODO RX restore services correctly.
    [Service (Exported = false)]
    public class GcmService : Service
    {
        private static readonly string Tag = "GcmService";

        public GcmService () : base ()
        {
        }

        public GcmService (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base (javaRef, transfer)
        {
        }

        //private Subscription<SyncFinishedMessage> subscriptionSyncFinishedMessage;
        private Intent wakelockIntent;
        private int? lastStartId;
        private DateTime? lastSyncTime;
        private bool needsResync;

        private void UpdateWakelockIntent (Intent intent)
        {
            if (intent == null) {
                return;
            }
            ClearWakelockIntent ();
            wakelockIntent = intent;
        }

        private void UpdateLastStartId (int startId)
        {
            ClearLastStartId ();
            lastStartId = startId;
        }

        private void ClearWakelockIntent ()
        {
            if (wakelockIntent != null) {
                GcmBroadcastReceiver.CompleteWakefulIntent (wakelockIntent);
                wakelockIntent = null;
            }
        }

        private void ClearLastStartId ()
        {
            if (lastStartId.HasValue) {
                StopSelfResult (lastStartId.Value);
                lastStartId = null;
            }
        }

        private void ScheduleSync ()
        {
            needsResync = true;
        }

        private void SyncOrStop (bool checkRunning = true)
        {
            /*
            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            // Need to check IsRunning, as it will tell us if the sync actually has finished
            if (checkRunning && syncManager.IsRunning) {
                return;
            }

            // See if we need to start the sync again
            if (needsResync) {
                needsResync = false;
                syncManager.Run (SyncMode.Pull);
                // If sync was successfully started we cannot stop yet
                if (syncManager.IsRunning) {
                    lastSyncTime = Time.UtcNow;
                    return;
                }
            }
            */
            // Stop the service:
            ClearLastStartId ();
            ClearWakelockIntent ();
        }

        public override void OnStart (Intent intent, int startId)
        {
            UpdateWakelockIntent (intent);
            UpdateLastStartId (startId);

            try {
                // Check if we need can skip sync
                /*
                if (intent != null && intent.Extras != null) {
                    var extras = intent.Extras;
                    var entryId = Convert.ToInt64 (extras.GetString ("task_id", String.Empty));
                    // updated_at is null (usually) when the time entry was just created, in that
                    // case we assign modifiedAt the default DateTime value (start of time)
                    var modifiedAt = ParseDate (extras.GetString ("updated_at", String.Empty));

                    var dataStore = ServiceContainer.Resolve<IDataStore> ();
                    var rows = await dataStore.Table<TimeEntryData> ().Where (r => r.RemoteId == entryId).ToListAsync ();
                    var entry = rows.FirstOrDefault ();

                    // Make sure that we need to start sync
                    var localDataNewer = entry != null && modifiedAt <= entry.ModifiedAt.ToUtc ();
                    var shouldBeSynced = lastSyncTime.HasValue && modifiedAt < lastSyncTime.Value;

                    if (localDataNewer || shouldBeSynced) {
                        return;
                    }
                }
                */
                ScheduleSync ();
            } catch (Exception exc) {
                // Log errors
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (Tag, exc, "Failed to process pushed message.");
            } finally {
                SyncOrStop ();
            }
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

        public override void OnCreate ()
        {
            base.OnCreate ();

            ((AndroidApp)Application).InitializeComponents ();

            //var bus = ServiceContainer.Resolve<MessageBus> ();
            //subscriptionSyncFinishedMessage = bus.Subscribe<SyncFinishedMessage> (OnSyncFinishedMessage);
        }

        public override void OnDestroy ()
        {
            /*
            if (subscriptionSyncFinishedMessage != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncFinishedMessage);
                subscriptionSyncFinishedMessage = null;
            }
            */
            base.OnDestroy ();
        }

        /*
        private void OnSyncFinishedMessage (SyncFinishedMessage msg)
        {
            SyncOrStop (checkRunning: false);
        }
        */
        private static DateTime ParseDate (string value)
        {
            DateTime dt;
            DateTime.TryParse (value, out dt);
            return dt.ToUtc ();
        }
    }
}
