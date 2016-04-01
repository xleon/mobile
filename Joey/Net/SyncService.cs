using System;
using Android.App;
using Android.Content;
using Android.OS;

namespace Toggl.Joey.Net
{
    // TODO RX restore services correctly.
    [Service (Exported = false)]
    public class SyncService : Service
    {
        //Subscription<SyncFinishedMessage> subscriptionSyncFinished;

        public SyncService () : base ()
        {
        }

        public SyncService (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base (javaRef, transfer)
        {
        }

        protected override void Dispose (bool disposing)
        {
            /*
            if (disposing) {
                if (subscriptionSyncFinished != null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    bus.Unsubscribe (subscriptionSyncFinished);
                    subscriptionSyncFinished = null;
                }
            }
            */
            base.Dispose (disposing);
        }

        public override void OnStart (Intent intent, int startId)
        {
            /*
            if (subscriptionSyncFinished != null) {
                return;
            }

            ((AndroidApp)Application).InitializeComponents ();


            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);

            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            if (!syncManager.IsRunning) {
                StopSelf ();

                if (subscriptionSyncFinished != null) {
                    bus.Unsubscribe (subscriptionSyncFinished);
                    subscriptionSyncFinished = null;
                }
            }
            */
        }

        public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
        {
            OnStart (intent, startId);
            return StartCommandResult.NotSticky;
        }

        public override IBinder OnBind (Intent intent)
        {
            return null;
        }

        /*
        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }

            if (subscriptionSyncFinished != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }

            StopSelf ();
        }
        */
    }
}

