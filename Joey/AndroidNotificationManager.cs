using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Activities;
using Android.Support.V4.App;

namespace Toggl.Joey
{
    class AndroidNotificationManager
    {
        #pragma warning disable 0414
        private readonly object subscriptionModelChanged;
        #pragma warning restore 0414

        private const int runningTimeEntryNotificationId = 10;

        private Context ctx;

        private NotificationCompat.Builder notificationBuilder;
        private NotificationManager notificationManager;

        private TimeEntryModel currentTimeEntry;

        public AndroidNotificationManager ()
        {
            ctx = ServiceContainer.Resolve<Context> ();
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
            CreateNotification ();
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

        private static bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }


        private void CreateNotification ()
        {
            PendingIntent stopIntent;

            PendingIntent editIntent;

            Intent resultIntent = new Intent(ctx, typeof(TimeEntriesActivity));

            TaskStackBuilder stackBuilder = TaskStackBuilder.Create(ctx);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(TimeEntriesActivity)));
            stackBuilder.AddNextIntent(resultIntent);

            PendingIntent resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

            stopIntent = editIntent = resultPendingIntent; //TODO Implement it

            notificationBuilder = new NotificationCompat.Builder (ctx)
                .SetAutoCancel(false)
                .SetUsesChronometer (true)
                .SetOngoing (true)
                .AddAction (Resource.Drawable.IcActionStop, "Stop", stopIntent)
                .AddAction (Resource.Drawable.IcActionEdit, "Edit", editIntent)
                .SetContentIntent (resultPendingIntent);
        }


        private void UpdateNotification (TimeEntryModel model)
        {
            notificationBuilder
                .SetSmallIcon(Resource.Drawable.IcNotificationIcon)
                .SetContentTitle(model.Project.Name)
                .SetContentText(model.Description)
                .SetWhen(GetUnixTime(model.StartTime));

            notificationManager.Notify(runningTimeEntryNotificationId, notificationBuilder.Build());
        }

        void CancelNotification ()
        {
            notificationManager.Cancel (runningTimeEntryNotificationId);
        }

        private long GetUnixTime (DateTime startTime)
        {
            TimeSpan t = startTime.ToUtc() - new DateTime(1970, 1, 1, 0, 0, 0);
            return (long) t.TotalMilliseconds;
        }
    }
}

