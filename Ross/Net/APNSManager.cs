using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.Net
{
    public class APNSManager
    {
        private static readonly string PushDeviceTokenKey = "PushDeviceToken";
        private static readonly string Tag = "APNSManager";
        private static readonly NSString updatedAtConst = new NSString ("updated_at");
        private static readonly NSString taskIdConst = new NSString ("task_id");

        private Subscription<AuthChangedMessage> subscriptionAuthChanged;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;

        private string userToken;
        private DateTime? lastSyncTime;
        private Action<UIBackgroundFetchResult> backgroundFetchHandler;

        private static string SavedDeviceToken
        {
            get {
                return NSUserDefaults.StandardUserDefaults.StringForKey (PushDeviceTokenKey);
            }
        }

        private static bool APNsIsEnabled
        {
            get {
                if (UIDevice.CurrentDevice.CheckSystemVersion (8, 0)) {
                    return UIApplication.SharedApplication.IsRegisteredForRemoteNotifications;
                } else {
                    var types = UIApplication.SharedApplication.EnabledRemoteNotificationTypes;
                    return types != UIRemoteNotificationType.None;
                }
            }
        }

        public APNSManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChangedMessage);
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
        }

        public void RegisteredForRemoteNotifications (UIApplication application, NSData token)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();

            var deviceToken = token.Description;
            if (!string.IsNullOrWhiteSpace (deviceToken)) {
                deviceToken = deviceToken.Trim ('<').Trim ('>').Replace (" ", string.Empty);
            }

            var oldDeviceToken = NSUserDefaults.StandardUserDefaults.StringForKey (PushDeviceTokenKey);
            if (oldDeviceToken != deviceToken) {
                RegisterDeviceOnTogglService (deviceToken, authManager.Token);
            }

            if (!string.IsNullOrEmpty (oldDeviceToken)) {
                UnregisterDeviceFromTogglService (oldDeviceToken, authManager.Token);
            }

            NSUserDefaults.StandardUserDefaults.SetString (deviceToken, PushDeviceTokenKey);
        }

        public void FailedToRegisterForRemoteNotifications (UIApplication application, NSError error)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            log.Info (Tag, "Failed To Register For Remote Notifications (APNS)");
        }

        public async Task DidReceiveRemoteNotification (UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            backgroundFetchHandler = completionHandler;

            try {
                var syncManager = ServiceContainer.Resolve<ISyncManager> ();

                NSObject entryIdObj, modifiedAtObj;
                userInfo.TryGetValue (updatedAtConst, out modifiedAtObj);
                userInfo.TryGetValue (taskIdConst, out entryIdObj);

                var entryId = Convert.ToInt64 (entryIdObj.ToString ());

                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                var rows = await dataStore.Table<TimeEntryData> ()
                           .QueryAsync (r => r.RemoteId == entryId);
                var entry = rows.FirstOrDefault();

                var modifiedAt = ParseDate (modifiedAtObj.ToString());

                var localDataNewer = entry != null && modifiedAt <= entry.ModifiedAt.ToUtc ();
                var skipSync = lastSyncTime.HasValue && modifiedAt < lastSyncTime.Value;

                if (syncManager.IsRunning || localDataNewer || skipSync) {
                    return;
                }

                syncManager.Run (SyncMode.Pull);
                if (syncManager.IsRunning) {
                    lastSyncTime = Time.UtcNow;
                }
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (Tag, ex, "Failed to process pushed message.");
            }
        }

        private void OnAuthChangedMessage (AuthChangedMessage msg)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();

            if (APNsIsEnabled && SavedDeviceToken != null) {
                if (authManager.IsAuthenticated) {
                    RegisterDeviceOnTogglService (SavedDeviceToken, authManager.Token);
                    userToken = authManager.Token;
                } else {
                    UnregisterDeviceFromTogglService (SavedDeviceToken, userToken);
                }
            } else if (authManager.IsAuthenticated) {
                RegisterDeviceOnAPNs ();
            }
        }

        private static void IgnoreTaskErrors (Task task)
        {
            task.ContinueWith (t => {
                var e = t.Exception;
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Info (Tag, e, "Failed to send APNS info/action to server.");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static void RegisterDeviceOnTogglService (string deviceToken, string authToken)
        {
            if (string.IsNullOrEmpty (deviceToken) || string.IsNullOrEmpty (authToken)) {
                return;
            }

            var pushClient = ServiceContainer.Resolve<IPushClient> ();
            IgnoreTaskErrors (pushClient.Register (authToken, PushService.APNS, deviceToken));
        }

        private static void UnregisterDeviceFromTogglService (string deviceToken, string authToken)
        {
            if (string.IsNullOrEmpty (deviceToken) || string.IsNullOrEmpty (authToken)) {
                return;
            }

            var pushClient = ServiceContainer.Resolve<IPushClient> ();
            IgnoreTaskErrors (pushClient.Unregister (authToken, PushService.APNS, deviceToken));
        }

        private static void RegisterDeviceOnAPNs()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion (8, 0)) {
                var pushSettings = UIUserNotificationSettings.GetSettingsForTypes (UIUserNotificationType.Badge, new NSSet ());
                UIApplication.SharedApplication.RegisterUserNotificationSettings (pushSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications ();
            } else {
                UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Badge;
                UIApplication.SharedApplication.RegisterForRemoteNotificationTypes (notificationTypes);
            }
        }

        private static DateTime ParseDate (string value)
        {
            if (value == null) {
                return DateTime.MaxValue;
            }
            DateTime dt;
            DateTime.TryParse (value, out dt);
            return dt.ToUtc ();
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            if (backgroundFetchHandler != null) {
                backgroundFetchHandler (msg.HadErrors ? UIBackgroundFetchResult.Failed : UIBackgroundFetchResult.NewData);
            }
        }

    }
}

