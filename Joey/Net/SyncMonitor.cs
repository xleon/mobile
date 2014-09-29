using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Android.Content;

namespace Toggl.Joey.Net
{
    public sealed class SyncMonitor : IDisposable
    {
        private Subscription<SyncStartedMessage> subscriptionSyncStarted;

        public SyncMonitor ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSyncStarted = bus.Subscribe<SyncStartedMessage> (OnSyncStarted);

            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            if (syncManager.IsRunning) {
                StartSyncService ();
            }
        }

        public void Dispose ()
        {
            if (subscriptionSyncStarted != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncStarted);
                subscriptionSyncStarted = null;
            }
            GC.SuppressFinalize (this);
        }

        private void StartSyncService ()
        {
            var ctx = ServiceContainer.Resolve<Context> ();
            var intent = new Intent (ctx, typeof (SyncService));
            ctx.StartService (intent);
        }

        private void OnSyncStarted (SyncStartedMessage msg)
        {
            StartSyncService ();
        }
    }
}
