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

        public override void OnStart (Intent intent, int startId)
        {
            try {
                var action = intent.Action;
                var widgetManager = ServiceContainer.Resolve<WidgetSyncManager>();

                if (action == WidgetProvider.StartStopAction) {
                    widgetManager.StartStopTimeEntry();
                } else if (action == WidgetProvider.ContiueAction) {

                    // Get entry Id string.
                    var entryId = intent.GetStringExtra (WidgetProvider.EntryIdParameter);
                    Guid entryGuid;
                    Guid.TryParse (entryId, out entryGuid);

                    // Set correct Guid.
                    var widgetUpdateService = ServiceContainer.Resolve<IWidgetUpdateService> ();
                    widgetUpdateService.EntryIdStarted = entryGuid;

                    widgetManager.ContinueTimeEntry();
                }
            } finally {
                WakefulBroadcastReceiver.CompleteWakefulIntent (intent);
                StopSelf (startId);
            }
        }

        private void LaunchApp()
        {
            var startAppIntent = new Intent (Intent.ActionMain)
            .AddCategory (Intent.CategoryLauncher)
            .AddFlags (ActivityFlags.NewTask)
            .SetComponent (
                new ComponentName (
                    ApplicationContext.PackageName,
                    typeof (Toggl.Joey.UI.Activities.MainDrawerActivity).FullName
                )
            );
            StartActivity (startAppIntent);
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
                serviceIntent.PutExtra (WidgetProvider.EntryIdParameter, intent.GetStringExtra (WidgetProvider.EntryIdParameter));
                StartWakefulService (context, serviceIntent);
            }
        }
    }
}