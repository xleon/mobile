using System;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using XPlatUtils;

namespace Toggl.Joey.Widget
{
    [BroadcastReceiver (Label = "@string/WidgetName")]
    [IntentFilter (new  [] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData ("android.appwidget.provider", Resource = "@xml/widget_info")]

    public class WidgetProvider : AppWidgetProvider
    {
        public const string TimeEntryIdParameter = "entryId";
        public const string StartStopAction = "com.toggl.timer.widget.START_ENTRY";
        public const string ContiueAction = "com.toggl.timer.widget.CONTINUE_ENTRY";
        public const string RefreshListAction = "com.toggl.timer.widget.REFRESH_CONTENT";
        public const string RefreshCompleteAction = "com.toggl.timer.widget.REFRESH_COMPLETE";
        private const string ThreadWorkerName = "com.toggl.timer.widgetprovider";

        private static HandlerThread workerThread;
        private static Handler workerQueue;

        public WidgetProvider()
        {
            // Start the worker thread
            workerThread = new HandlerThread (ThreadWorkerName);
            workerThread.Start();
            workerQueue = new Handler (workerThread.Looper);
        }

        public override void OnUpdate (Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            // Request widget update.
            var serviceIntent = new Intent (context, typeof (InitWidgetService));
            context.StartService (serviceIntent);

            // Setup widget UI.
            SetupWidget (context);

            base.OnUpdate (context, appWidgetManager, appWidgetIds);
        }

        public override void OnReceive (Context context, Intent intent)
        {
            String action = intent.Action;
            /*
            if (action == RefreshListAction && UpdateService.IsUserLogged) {
                ScheduleUpdate (context, action);
            }

            if (action == RefreshCompleteAction) {
                ScheduleUpdate (context, action);
            }
            */
            base.OnReceive (context, intent);
        }

        private void SetupWidget (Context ctx)
        {
            RemoteViews views;
            /*
            var wm = AppWidgetManager.GetInstance (ctx);
            var cn = new ComponentName (ctx, Java.Lang.Class.FromType (typeof (WidgetProvider)));
            var ids = wm.GetAppWidgetIds (cn);

            if (UpdateService.IsUserLogged) {

                views = new RemoteViews (ctx.PackageName, Resource.Layout.keyguard_widget);

                SetupRunningBtn (ctx, views);

                var adapterServiceIntent = new Intent (ctx, typeof (RemotesViewsFactoryService));
                adapterServiceIntent.PutExtra (AppWidgetManager.ExtraAppwidgetIds, ids);
                adapterServiceIntent.SetData (Android.Net.Uri.Parse (adapterServiceIntent.ToUri (IntentUriType.Scheme)));

                for (int i = 0; i < ids.Length; i++) {
                    views.SetRemoteAdapter (ids[i], Resource.Id.WidgetRecentEntriesListView, adapterServiceIntent);
                }

                var listItemIntent = new Intent (ctx, typeof (WidgetStartStopService.Receiver));
                listItemIntent.SetData (Android.Net.Uri.Parse (listItemIntent.ToUri (IntentUriType.Scheme)));
                var pendingIntent = PendingIntent.GetBroadcast (ctx, 0, listItemIntent, PendingIntentFlags.UpdateCurrent);
                views.SetPendingIntentTemplate (Resource.Id.WidgetRecentEntriesListView, pendingIntent);
                views.SetOnClickPendingIntent (Resource.Id.WidgetActionButton, StartStopButtonIntent (ctx));

            } else {
                views = new RemoteViews (ctx.PackageName, Resource.Layout.widget_login);
                views.SetOnClickPendingIntent (Resource.Id.WidgetLoginButton, LogInButtonIntent (ctx));
            }

            // Update widget view.
            wm.UpdateAppWidget (ids, views);
            */
        }

        private void SetupRunningBtn (Context ctx, RemoteViews views)
        {
            /*
            var entry = new WidgetSyncManager.WidgetEntryData();
            var isRunning = false;

            // Check if an entry is running.
            foreach (var item in UpdateService.LastEntries)
                if (item.IsRunning) {
                    entry = item;
                    isRunning = true;
                }

            var baseTime = SystemClock.ElapsedRealtime ();

            if (isRunning) {
                views.SetInt (Resource.Id.WidgetActionButton, "setBackgroundColor", ctx.Resources.GetColor (Resource.Color.bright_red));
                views.SetInt (Resource.Id.WidgetActionButton, "setText", Resource.String.TimerStopButtonText);
                views.SetInt (Resource.Id.WidgetColorView, "setColorFilter", Color.ParseColor (entry.Color));
                views.SetViewVisibility (Resource.Id.WidgetRunningEntry, ViewStates.Visible);
                views.SetTextViewText (
                    Resource.Id.WidgetRunningDescriptionTextView,
                    String.IsNullOrWhiteSpace (entry.Description) ? ctx.Resources.GetString (Resource.String.RunningWidgetNoDescription) : entry.Description);

                var time = (long)entry.Duration.TotalMilliseconds;

                // Format chronometer correctly.
                string format = "00:%s";
                if (time >= 3600000 && time < 36000000) {
                    format = "0%s";
                } else if (time >= 36000000) {
                    format = "%s";
                }

                views.SetChronometer (Resource.Id.Chronometer, baseTime - (long)entry.Duration.TotalMilliseconds, format, true);
            } else {
                views.SetInt (Resource.Id.WidgetActionButton, "setBackgroundColor", ctx.Resources.GetColor (Resource.Color.bright_green));
                views.SetInt (Resource.Id.WidgetActionButton, "setText", Resource.String.TimerStartButtonText);
                views.SetViewVisibility (Resource.Id.WidgetRunningEntry, ViewStates.Invisible);
                views.SetChronometer (Resource.Id.Chronometer, baseTime, "00:%s", false);
                views.SetTextViewText (Resource.Id.Chronometer, "00:00:00");
            }
            */
        }

        private PendingIntent StartStopButtonIntent (Context ctx)
        {
            var intent = new Intent (ctx, typeof (WidgetStartStopService.Receiver));
            intent.SetAction (WidgetProvider.StartStopAction);
            return PendingIntent.GetBroadcast (ctx, 0, intent, PendingIntentFlags.UpdateCurrent);
        }

        private PendingIntent LogInButtonIntent (Context ctx)
        {
            var loginIntent = new Intent (Intent.ActionMain)
            .AddCategory (Intent.CategoryLauncher)
            .AddFlags (ActivityFlags.NewTask)
            .SetComponent (new ComponentName (ctx.PackageName, "toggl.joey.ui.activities.MainDrawerActivity"));
            return PendingIntent.GetActivity (ctx, 0, loginIntent, PendingIntentFlags.UpdateCurrent);
        }


        private void ScheduleUpdate (Context ctx, string action)
        {
            // Adds a runnable to update the widgets in the worker queue.
            workerQueue.RemoveMessages (0);
            workerQueue.Post (() => {

                var wm = AppWidgetManager.GetInstance (ctx);
                var cn = new ComponentName (ctx, Java.Lang.Class.FromType (typeof (WidgetProvider)));
                var ids = wm.GetAppWidgetIds (cn);

                if (action == RefreshCompleteAction) {
                    OnUpdate (ctx, wm, ids);
                } else {
                    var views = new RemoteViews (ctx.PackageName, Resource.Layout.keyguard_widget);
                    SetupRunningBtn (ctx, views);
                    wm.PartiallyUpdateAppWidget (ids, views);
                    wm.NotifyAppWidgetViewDataChanged (ids, Resource.Id.WidgetRecentEntriesListView);
                }
            });
        }

        public static void RefreshWidget (Context ctx, string action)
        {
            // Sends a request to the rich push message to refresh.
            RefreshWidget (ctx, action, 0);
        }

        public static void RefreshWidget (Context ctx, string action, long delayInMs)
        {
            //Sends a request to the rich push message to refresh with a delay.
            var refreshIntent = new Intent (ctx, typeof (WidgetProvider));
            refreshIntent.SetAction (action);

            if (delayInMs > 0) {
                PendingIntent pendingIntent = PendingIntent.GetBroadcast (ctx, 0, refreshIntent, 0);
                var am = (AlarmManager) ctx.GetSystemService (Context.AlarmService);
                am.Set (AlarmType.RtcWakeup, (long) new TimeSpan (DateTime.Now.Ticks).TotalMilliseconds + delayInMs, pendingIntent);
            } else {
                ctx.SendBroadcast (refreshIntent);
            }
        }
    }
}
