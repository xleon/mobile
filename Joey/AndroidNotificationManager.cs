using System;
using System.Linq;
using Android.App;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Activities;
using NotificationCompat = Android.Support.V4.App.NotificationCompat;

namespace Toggl.Joey
{
    class AndroidNotificationManager
    {
        #pragma warning disable 0414
        private readonly object subscriptionModelChanged;
        #pragma warning restore 0414

        private const int RunningTimeEntryNotifId = 42;
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        private Context ctx;
        private NotificationCompat.Builder notificationBuilder;
        private NotificationManager notificationManager;
        private TimeEntryModel currentTimeEntry;

        public AndroidNotificationManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);

            ctx = ServiceContainer.Resolve<Context> ();
            notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
            CreateNotificationBuilder ();
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            var entry = msg.Model as TimeEntryModel;
            if (entry == null)
                return;

            if (currentTimeEntry == entry) {
                if (msg.PropertyName == TimeEntryModel.PropertyIsRunning
                    || msg.PropertyName == TimeEntryModel.PropertyDescription
                    || msg.PropertyName == TimeEntryModel.PropertyStartTime) {

                    if (entry.IsRunning) {
                        // Description or StartTime of current TW were changed, need to upadte info
                        UpdateNotification (entry);
                    } else {
                        // Current TE was stopped
                        CancelNotification ();
                    }
                }
            } else {
                if (msg.PropertyName == TimeEntryModel.PropertyIsRunning
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                    if (ForCurrentUser (entry) && entry.IsRunning) {
                        // New TE was started by current user
                        UpdateNotification (entry);
                        currentTimeEntry = entry;
                    }
                }
            }

        }

        private bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }

        private void CreateNotificationBuilder ()
        {
            Intent openTimeEntriesActivityIntent = new Intent(ctx, typeof(TimeEntriesActivity));
            openTimeEntriesActivityIntent.SetAction(Intent.ActionMain);
            openTimeEntriesActivityIntent.AddCategory(Intent.CategoryLauncher);
            PendingIntent contentIntent = PendingIntent.GetActivity(ctx, 0, openTimeEntriesActivityIntent, 0);
            var res = ctx.Resources;

            var closeRunningTimeEmtryIntent = new Intent(ctx, typeof(StopTimeEntryBroadcastReceiver));

            var pendingIntentClose = PendingIntent.GetBroadcast (ctx, 0, closeRunningTimeEmtryIntent, PendingIntentFlags.UpdateCurrent);

            notificationBuilder = new NotificationCompat.Builder (ctx)
                .SetAutoCancel (false)
                .SetUsesChronometer (true)
                .SetOngoing (true)
                .AddAction (Resource.Drawable.IcActionStop, res.GetString (Resource.String.RunningNotificationStopButton), pendingIntentClose)
//                .AddAction (Resource.Drawable.IcActionEdit, res.GetString (Resource.String.RunningNotificationEditButton), editIntent)
                .SetContentIntent (contentIntent);
        }

        private void UpdateNotification (TimeEntryModel model)
        {
            string projectName;
            if (model.Project != null) {
                projectName = model.Project.Name;
            } else {
                projectName = ctx.Resources.GetString (Resource.String.RunningNotificationNoProject);
            }

            string entryDescription = model.Description;
            if (String.IsNullOrWhiteSpace (entryDescription)) {
                entryDescription = ctx.Resources.GetString (Resource.String.RunningNotificationNoDescription);
            }

            notificationBuilder
                .SetSmallIcon (Resource.Drawable.IcNotificationIcon)
                .SetContentTitle (projectName)
                .SetContentText (entryDescription)
                .SetWhen (GetUnixTime (model.StartTime));

            notificationManager.Notify(RunningTimeEntryNotifId, notificationBuilder.Build());
        }

        void CancelNotification ()
        {
            notificationManager.Cancel (RunningTimeEntryNotifId);
        }

        private long GetUnixTime (DateTime startTime)
        {
            TimeSpan t = startTime.ToUtc() - UnixStart;
            return (long) t.TotalMilliseconds;
        }
    }
}

