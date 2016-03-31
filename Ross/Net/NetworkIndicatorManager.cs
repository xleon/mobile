using System;
using UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Ross.Net
{
    public class NetworkIndicatorManager : IDisposable
    {
        //private Subscription<SyncStartedMessage> subscriptionSyncStarted;
        //private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private bool syncRunning;

        public NetworkIndicatorManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            //subscriptionSyncStarted = bus.Subscribe<SyncStartedMessage> (OnSyncStarted);
            //subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);

            //var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            //syncRunning = syncManager.IsRunning;
            ResetIndicator ();
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
        }
        /*
        private void OnSyncStarted (SyncStartedMessage msg)
        {
            syncRunning = true;
            ResetIndicator ();
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            syncRunning = false;
            ResetIndicator ();
        }
        */
        private void ResetIndicator ()
        {
            UIApplication.SharedApplication.NetworkActivityIndicatorVisible = syncRunning;
        }
    }
}
