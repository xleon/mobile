using System;
using System.Threading.Tasks;
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
        private Subscription<SyncWidgetMessage> subscriptionSyncFinishedMessage;

        public InitWidgetService ()
        {
        }

        public InitWidgetService (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base (javaRef, transfer)
        {
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

        public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
        {
            Task.Run (async () => await ServiceContainer.Resolve<WidgetSyncManager>().SyncWidgetData ());
            return StartCommandResult.Sticky;
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