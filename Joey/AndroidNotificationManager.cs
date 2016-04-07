using System;
using Android.App;
using Android.Content;
using Toggl.Joey.UI.Activities;
using XPlatUtils;
using NotificationCompat = Android.Support.V4.App.NotificationCompat;

namespace Toggl.Joey
{
    public class AndroidNotificationManager : IDisposable
    {
        private const int IdleNotifId = 40;
        private const int RunningNotifId = 42;
        private readonly Context ctx;
        private readonly NotificationManager notificationManager;
        private readonly NotificationCompat.Builder runningBuilder;
        private readonly NotificationCompat.Builder idleBuilder;

        public AndroidNotificationManager()
        {
            ctx = ServiceContainer.Resolve<Context> ();
            notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
            runningBuilder = CreateRunningNotificationBuilder(ctx);
            idleBuilder = CreateIdleNotificationBuilder(ctx);
        }

        public void Dispose()
        {
        }

        private void SyncNotification()
        {
            /*
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (!authManager.IsAuthenticated) {
                notificationManager.Cancel (RunningNotifId);
                notificationManager.Cancel (IdleNotifId);
            } else if (currentTimeEntry.State != TimeEntryState.Running) {
                notificationManager.Cancel (RunningNotifId);
                var settings = ServiceContainer.Resolve<SettingsStore> ();
                if (settings.IdleNotification) {
                    notificationManager.Notify (IdleNotifId, idleBuilder.Build ());
                } else {
                    notificationManager.Cancel (IdleNotifId);
                }
            } else {
                var settings = ServiceContainer.Resolve<SettingsStore> ();
                if (!settings.ShowNotification) {
                    notificationManager.Cancel (RunningNotifId);
                    notificationManager.Cancel (IdleNotifId);
                    return;
                }
                notificationManager.Cancel (IdleNotifId);
                var correction = ServiceContainer.Resolve<TimeCorrectionManager> ().Correction;
                var startTime = currentTimeEntry.StartTime - correction;
                runningBuilder
            <<<<<<< 9f9949da47fe06321c5b025b2225218699fe8746
                .SetContentTitle (GetProjectName (currentTimeEntry))
                .SetContentText (GetDescription (currentTimeEntry))
                .SetWhen ((long)startTime.ToUnix ().TotalMilliseconds);
            =======
                    .SetContentTitle (GetProjectName ())
                    .SetContentText (GetDescription ())
                    .SetWhen ((long)startTime.ToUnix ().TotalMilliseconds);
            >>>>>>> Removed ActiveTimeEntryManager and deactivated Notification, Widget and Wear.

                notificationManager.Notify (RunningNotifId, runningBuilder.Build ());
            }
            */
        }

        private string GetProjectName()
        {
            return ctx.Resources.GetString(Resource.String.RunningNotificationNoProject);
        }

        private string GetDescription()
        {
            return ctx.Resources.GetString(Resource.String.RunningNotificationNoDescription);
        }

        private static NotificationCompat.Builder CreateRunningNotificationBuilder(Context ctx)
        {
            var res = ctx.Resources;

            var openIntent = new Intent(ctx, typeof(MainDrawerActivity));
            openIntent.SetAction(Intent.ActionMain);
            openIntent.AddCategory(Intent.CategoryLauncher);
            var pendingOpenIntent = PendingIntent.GetActivity(ctx, 0, openIntent, 0);

            var stopIntent = new Intent(ctx, typeof(StopRunningTimeEntryService.Receiver));
            var pendingStopIntent = PendingIntent.GetBroadcast(ctx, 0, stopIntent, PendingIntentFlags.UpdateCurrent);

            return new NotificationCompat.Builder(ctx)
                   .SetAutoCancel(false)
                   .SetUsesChronometer(true)
                   .SetOngoing(true)
                   .SetSmallIcon(Resource.Drawable.IcNotificationIcon)
                   // TODO: Removed Stop button from notification until
                   // find a fiable solution
                   // .AddAction (Resource.Drawable.IcActionStop, res.GetString (Resource.String.RunningNotificationStopButton), pendingStopIntent)
                   // .AddAction (Resource.Drawable.IcActionEdit, res.GetString (Resource.String.RunningNotificationEditButton), editIntent)
                   .SetContentIntent(pendingOpenIntent);
        }

        private static NotificationCompat.Builder CreateIdleNotificationBuilder(Context ctx)
        {
            var res = ctx.Resources;

            var openIntent = new Intent(ctx, typeof(MainDrawerActivity));
            openIntent.SetAction(Intent.ActionMain);
            openIntent.AddCategory(Intent.CategoryLauncher);
            var pendingOpenIntent = PendingIntent.GetActivity(ctx, 0, openIntent, 0);

            return new NotificationCompat.Builder(ctx)
                   .SetAutoCancel(false)
                   .SetOngoing(true)
                   .SetSmallIcon(Resource.Drawable.IcNotificationIconIdle)
                   .SetContentIntent(pendingOpenIntent)
                   .SetContentTitle(res.GetString(Resource.String.IdleNotificationTitle))
                   .SetContentText(res.GetString(Resource.String.IdleNotificationText));
        }
    }
}