using System;
using System.Linq;
using Android.App;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Activities;
using NotificationCompat = Android.Support.V4.App.NotificationCompat;

namespace Toggl.Joey
{
    sealed class AndroidNotificationManager : IDisposable
    {
        private const int IdleNotifId = 40;
        private const int RunningNotifId = 42;
        private readonly Context ctx;
        private readonly NotificationManager notificationManager;
        private readonly NotificationCompat.Builder runningBuilder;
        private readonly NotificationCompat.Builder idleBuilder;
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;
        private TimeEntryModel currentTimeEntry;

        public AndroidNotificationManager ()
        {
            ctx = ServiceContainer.Resolve<Context> ();
            notificationManager = (NotificationManager)ctx.GetSystemService (Context.NotificationService);
            runningBuilder = CreateRunningNotificationBuilder (ctx);
            idleBuilder = CreateIdleNotificationBuilder (ctx);

            currentTimeEntry = TimeEntryModel.FindRunning ();
            SyncNotification ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (OnSettingChanged);
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionModelChanged != null) {
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }
            if (subscriptionSettingChanged != null) {
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }
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

        private void OnSettingChanged (SettingChangedMessage msg)
        {
            if (msg.Name == SettingsStore.PropertyIdleNotification) {
                SyncNotification ();
            }
        }

        private bool ForCurrentUser (TimeEntryModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return model.UserId == authManager.UserId;
        }

        private void SyncNotification ()
        {
            if (currentTimeEntry == null) {
                notificationManager.Cancel (RunningNotifId);
                var settings = ServiceContainer.Resolve<SettingsStore> ();
                if (settings.IdleNotification) {
                    notificationManager.Notify (IdleNotifId, idleBuilder.Build ());
                } else {
                    notificationManager.Cancel (IdleNotifId);
                }
            } else {
                notificationManager.Cancel (IdleNotifId);
                runningBuilder
                    .SetContentTitle (GetProjectName (currentTimeEntry))
                    .SetContentText (GetDescription (currentTimeEntry))
                    .SetWhen ((long)currentTimeEntry.StartTime.ToUnix ().TotalMilliseconds);

                notificationManager.Notify (RunningNotifId, runningBuilder.Build ());
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

        private static NotificationCompat.Builder CreateRunningNotificationBuilder (Context ctx)
        {
            var res = ctx.Resources;

            var openIntent = new Intent (ctx, typeof(MainDrawerActivity));
            openIntent.SetAction (Intent.ActionMain);
            openIntent.AddCategory (Intent.CategoryLauncher);
            var pendingOpenIntent = PendingIntent.GetActivity (ctx, 0, openIntent, 0);

            var stopIntent = new Intent (ctx, typeof(StopTimeEntryBroadcastReceiver));
            var pendingStopIntent = PendingIntent.GetBroadcast (ctx, 0, stopIntent, PendingIntentFlags.UpdateCurrent);

            return new NotificationCompat.Builder (ctx)
                    .SetAutoCancel (false)
                    .SetUsesChronometer (true)
                    .SetOngoing (true)
                    .SetSmallIcon (Resource.Drawable.IcNotificationIcon)
                    .AddAction (Resource.Drawable.IcActionStop, res.GetString (Resource.String.RunningNotificationStopButton), pendingStopIntent)
//                    .AddAction (Resource.Drawable.IcActionEdit, res.GetString (Resource.String.RunningNotificationEditButton), editIntent)
                    .SetContentIntent (pendingOpenIntent);
        }

        private static NotificationCompat.Builder CreateIdleNotificationBuilder (Context ctx)
        {
            var res = ctx.Resources;

            var openIntent = new Intent (ctx, typeof(MainDrawerActivity));
            openIntent.SetAction (Intent.ActionMain);
            openIntent.AddCategory (Intent.CategoryLauncher);
            var pendingOpenIntent = PendingIntent.GetActivity (ctx, 0, openIntent, 0);

            return new NotificationCompat.Builder (ctx)
                    .SetAutoCancel (false)
                    .SetOngoing (true)
                    .SetSmallIcon (Resource.Drawable.IcNotificationIcon)
                    .SetContentIntent (pendingOpenIntent)
                    .SetContentTitle (res.GetString (Resource.String.IdleNotificationTitle))
                    .SetContentText (res.GetString (Resource.String.IdleNotificationText));
        }
    }
}
