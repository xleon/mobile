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
        private readonly Subscription<ModelChangedMessage> subscriptionModelChanged;
        #pragma warning restore 0414

        private const int RunningTimeEntryNotifId = 42;
        private readonly Context ctx;
        private readonly NotificationManager notificationManager;
        private readonly NotificationCompat.Builder notificationBuilder;
        private TimeEntryModel currentTimeEntry;

        public AndroidNotificationManager ()
        {
            ctx = ServiceContainer.Resolve<Context> ();
            notificationManager = (NotificationManager)ctx.GetSystemService (Context.NotificationService);
            notificationBuilder = CreateNotificationBuilder (ctx);

            currentTimeEntry = TimeEntryModel.FindRunning ();
            SyncNotification ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (currentTimeEntry == msg.Model) {
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyDescription
                    || msg.PropertyName == TimeEntryModel.PropertyStartTime
                    || msg.PropertyName == TimeEntryModel.PropertyProjectId
                    || msg.PropertyName == TimeEntryModel.PropertyTaskId
                    || msg.PropertyName == TimeEntryModel.PropertyUserId) {

                    if (currentTimeEntry.State != TimeEntryState.Running || !ForCurrentUser (currentTimeEntry)) {
                        currentTimeEntry = null;
                    }

                    SyncNotification ();
                }
            } else if (currentTimeEntry != null
                       && msg.Model.Id == currentTimeEntry.ProjectId
                       && msg.Model == currentTimeEntry.Project) {
                if (msg.PropertyName == ProjectModel.PropertyName
                    || msg.PropertyName == ProjectModel.PropertyClientId) {
                    SyncNotification ();
                }
            } else if (currentTimeEntry != null
                       && currentTimeEntry.ProjectId != null
                       && currentTimeEntry.Project != null
                       && msg.Model.Id == currentTimeEntry.Project.ClientId
                       && msg.Model == currentTimeEntry.Project.Client) {
                if (msg.PropertyName == ClientModel.PropertyName) {
                    SyncNotification ();
                }
            } else if (msg.Model is TimeEntryModel) {
                var entry = (TimeEntryModel)msg.Model;
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared
                    || msg.PropertyName == TimeEntryModel.PropertyIsPersisted
                    || msg.PropertyName == TimeEntryModel.PropertyUserId) {
                    if (entry.IsShared && entry.IsPersisted && entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                        currentTimeEntry = entry;
                        SyncNotification ();
                    }
                }
            }
        }

        private bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }

        private NotificationCompat.Builder CreateNotificationBuilder (Context ctx)
        {
            var res = ctx.Resources;

            var openIntent = new Intent (ctx, typeof(TimeTrackingActivity));
            openIntent.SetAction (Intent.ActionMain);
            openIntent.AddCategory (Intent.CategoryLauncher);
            var pendingOpenIntent = PendingIntent.GetActivity (ctx, 0, openIntent, 0);

            var closeIntent = new Intent (ctx, typeof(StopTimeEntryBroadcastReceiver));
            var pendingCloseIntent = PendingIntent.GetBroadcast (ctx, 0, closeIntent, PendingIntentFlags.UpdateCurrent);

            return new NotificationCompat.Builder (ctx)
                .SetAutoCancel (false)
                .SetUsesChronometer (true)
                .SetOngoing (true)
                .SetSmallIcon (Resource.Drawable.IcNotificationIcon)
                .AddAction (Resource.Drawable.IcActionStop, res.GetString (Resource.String.RunningNotificationStopButton), pendingCloseIntent)
//                .AddAction (Resource.Drawable.IcActionEdit, res.GetString (Resource.String.RunningNotificationEditButton), editIntent)
                .SetContentIntent (pendingOpenIntent);
        }

        private void SyncNotification ()
        {
            if (currentTimeEntry == null) {
                notificationManager.Cancel (RunningTimeEntryNotifId);
            } else {
                notificationBuilder
                    .SetContentTitle (GetProjectName (currentTimeEntry))
                    .SetContentText (GetDescription (currentTimeEntry))
                    .SetWhen ((long)currentTimeEntry.StartTime.ToUnix ().TotalMilliseconds);

                notificationManager.Notify (RunningTimeEntryNotifId, notificationBuilder.Build ());
            }
        }

        private string GetProjectName (TimeEntryModel entry)
        {
            if (entry == null) {
                return null;
            } else if (entry.Project != null) {
                return entry.Project.Name;
            } else {
                return ctx.Resources.GetString (Resource.String.RunningNotificationNoProject);
            }
        }

        private string GetDescription (TimeEntryModel entry)
        {
            string description = entry.Description;
            if (String.IsNullOrWhiteSpace (description)) {
                description = ctx.Resources.GetString (Resource.String.RunningNotificationNoDescription);
            }
            return description;
        }
    }
}
