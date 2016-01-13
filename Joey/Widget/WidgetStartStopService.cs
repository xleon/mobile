using System;
using Android.App;
using Android.Content;
using Android.Support.V4.Content;
using Toggl.Phoebe;
using XPlatUtils;

namespace Toggl.Joey.Widget
{
    [Service (Exported = false)]
    public sealed class WidgetStartStopService : Service
    {
        private static readonly string Tag = "WidgetStartStopService";

        public WidgetStartStopService ()
        {
        }

        public WidgetStartStopService (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base (javaRef, transfer)
        {
        }

        public async override void OnStart (Intent intent, int startId)
        {
            try {
                var action = intent.Action;
                var widgetManager = ServiceContainer.Resolve<WidgetSyncManager>();

                if (action == WidgetProvider.StartStopAction) {
                    await widgetManager.StartStopTimeEntry();
                } else if (action == WidgetProvider.ContiueAction) {

                    // Get entry Id string.
                    var entryId = intent.GetStringExtra (WidgetProvider.TimeEntryIdParameter);
                    Guid entryGuid;
                    Guid.TryParse (entryId, out entryGuid);

                    // Set correct Guid.
                    var widgetUpdateService = ServiceContainer.Resolve<IWidgetUpdateService> ();
                    widgetUpdateService.EntryIdStarted = entryGuid;
                    await widgetManager.ContinueTimeEntry (entryGuid);
                }
            } finally {
                WakefulBroadcastReceiver.CompleteWakefulIntent (intent);
                StopSelf (startId);
            }
        }

        public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
        {
            OnStart (intent, startId);
            return StartCommandResult.Sticky;
        }

        public override void OnCreate ()
        {
            base.OnCreate ();
            ((AndroidApp)Application).InitializeComponents ();
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
                var serviceIntent = new Intent (context, typeof (WidgetStartStopService));
                serviceIntent.SetAction (intent.Action);
                serviceIntent.PutExtra (WidgetProvider.TimeEntryIdParameter, intent.GetStringExtra (WidgetProvider.TimeEntryIdParameter));
                StartWakefulService (context, serviceIntent);
            }
        }
    }
}