using System;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.Widget
{

    [Service (Exported = false)]
    public class InitWidgetService : Service
    {
        public InitWidgetService ()
        {
        }

        public InitWidgetService (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base (javaRef, transfer)
        {
        }

        private Subscription<SyncWidgetMessage> subscriptionSyncFinishedMessage;

        public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
        {
            StartSync();
            return StartCommandResult.Sticky;
        }

        public override void OnCreate ()
        {
            base.OnCreate ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSyncFinishedMessage = bus.Subscribe<SyncWidgetMessage> (OnSyncFinishedMessage);
        }

        public override void OnDestroy ()
        {
            if (subscriptionSyncFinishedMessage != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncFinishedMessage);
                subscriptionSyncFinishedMessage = null;
            }

            base.OnDestroy ();
        }

        private void StartSync ()
        {
            var widgetManager = ServiceContainer.Resolve<WidgetSyncManager>();
            widgetManager.SyncWidgetData ();
        }

        private void OnSyncFinishedMessage (SyncWidgetMessage msg)
        {
            if (!msg.IsStarted) {
                WidgetProvider.RefreshWidget (this, WidgetProvider.RefreshListAction);
                StopSelf();
            }
        }

        public override IBinder OnBind (Intent intent)
        {
            return null;
        }
    }
}