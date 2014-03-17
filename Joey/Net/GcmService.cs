using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Gms.Gcm;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.Net
{
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

        private readonly List<ScheduledSync> schedule = new List<ScheduledSync> ();

        public override void OnStart (Intent intent, int startId)
        {
            ((AndroidApp)Application).InitializeComponents ();

            try {
                var extras = intent.Extras;
                var entryId = Convert.ToInt64 (extras.GetString ("task_id", String.Empty));
                // updated_at is null (usually) when the time entry was just created, in that
                // case we assign modifiedAt the default DateTime value (start of time)
                var modifiedAt = ParseDate (extras.GetString ("updated_at", String.Empty));

                var entry = Model.ByRemoteId<TimeEntryModel> (entryId);
                if (entry != null && modifiedAt <= entry.ModifiedAt) {
                    // We already have the latest data, can skip this:
                    GcmBroadcastReceiver.CompleteWakefulIntent (intent);
                    StopSelf (startId);
                    return;
                }

                // Purge finished syncs:
                schedule.RemoveAll ((s) => s.IsFinished);

                // See if we can add this task to running sync:
                var running = schedule.FirstOrDefault ((s) => s.IsRunning);
                if (running != null && running.StartTime >= modifiedAt) {
                    running.AddCommand (intent, startId);
                    return;
                }

                // Finally add it to the new sync:
                var upcoming = schedule.FirstOrDefault ((s) => !s.IsCancelled && !s.IsRunning && !s.IsFinished);
                if (upcoming == null) {
                    upcoming = new ScheduledSync (this);
                    schedule.Add (upcoming);
                }
                upcoming.AddCommand (intent, startId);
            } catch (Exception exc) {
                // Something went wrong, recover gracefully
                GcmBroadcastReceiver.CompleteWakefulIntent (intent);
                StopSelf (startId);

                // Log errors
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to process pushed message.");
            }
        }

        public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
        {
            OnStart (intent, startId);

            return StartCommandResult.RedeliverIntent;
        }

        public override Android.OS.IBinder OnBind (Intent intent)
        {
            return null;
        }

        public override void OnDestroy ()
        {
            foreach (var sync in schedule) {
                sync.Cancel ();
            }
            schedule.Clear ();
            base.OnDestroy ();
        }

        private static DateTime ParseDate (string value)
        {
            DateTime dt;
            DateTime.TryParse (value, out dt);
            return dt.ToUtc ();
        }

        private class ScheduledSync
        {
            private readonly List<Intent> intents = new List<Intent> ();
            private readonly List<int> commandIds = new List<int> ();
            private GcmService service;
            private Subscription<SyncFinishedMessage> subscriptionSyncFinishedMessage;

            public ScheduledSync (GcmService service)
            {
                this.service = service;
            }

            public void Cancel ()
            {
                service = null;
                IsCancelled = true;
            }

            public void AddCommand (Intent intent, int commandId)
            {
                if (IsFinished)
                    throw new InvalidOperationException ("Cannot add commands to a finished sync.");

                var shouldSchedule = commandIds.Count == 0;

                intents.Add (intent);
                commandIds.Add (commandId);

                if (shouldSchedule) {
                    Schedule ();
                }
            }

            private void OnSyncFinishedMessage (SyncFinishedMessage msg)
            {
                if (IsCancelled || IsRunning) {
                    Finish ();
                } else {
                    // Previous sync finished, need to start pull sync now.
                    Start ();
                }
            }

            private void Schedule ()
            {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionSyncFinishedMessage = bus.Subscribe<SyncFinishedMessage> (OnSyncFinishedMessage);

                Start ();
            }

            private void Start ()
            {
                if (IsRunning || IsFinished)
                    throw new InvalidOperationException ("Start can only be called once.");

                var syncManager = ServiceContainer.Resolve<SyncManager> ();

                if (!syncManager.IsRunning) {
                    IsRunning = true;
                    StartTime = DateTime.UtcNow;

                    syncManager.Run (SyncMode.Pull);
                }
            }

            private void Finish ()
            {
                if (!IsRunning && !IsCancelled)
                    throw new InvalidOperationException ("Finish can only be called when the sync is running.");

                IsRunning = false;
                IsFinished = true;

                if (subscriptionSyncFinishedMessage != null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    bus.Unsubscribe (subscriptionSyncFinishedMessage);
                    subscriptionSyncFinishedMessage = null;
                }

                foreach (var intent in intents) {
                    GcmBroadcastReceiver.CompleteWakefulIntent (intent);
                }
                intents.Clear ();

                if (service != null) {
                    foreach (var commandId in commandIds) {
                        service.StopSelf (commandId);
                    }
                }
                commandIds.Clear ();
            }

            public DateTime? StartTime { get; private set; }

            public bool IsRunning { get; private set; }

            public bool IsFinished { get; private set; }

            public bool IsCancelled { get; private set; }
        }
    }
}
