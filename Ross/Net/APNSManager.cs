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
        private static readonly string Tag = "APNSManager";
        private const string PushDeviceTokenKey = "PushDeviceToken";

        private string authToken;
        private string SavedDeviceToken
        {
            get {
                return NSUserDefaults.StandardUserDefaults.StringForKey (PushDeviceTokenKey);
            }
        }

        private DateTime? lastSyncTime;

        public APNSManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Subscribe<AuthChangedMessage> (OnAuthChangedMessage);

            var authManager = ServiceContainer.Resolve<AuthManager> ();
            authToken = authManager.Token;
        }

        private bool CheckIfAPNSEnabled()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion (8, 0)) {
                return UIApplication.SharedApplication.IsRegisteredForRemoteNotifications;
            } else {
                var types = UIApplication.SharedApplication.EnabledRemoteNotificationTypes;
                return types != UIRemoteNotificationType.None;
            }
        }

        private void OnAuthChangedMessage (AuthChangedMessage msg)
        {
            var apnsEnabled = CheckIfAPNSEnabled();
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            authToken = authManager.Token;

            if (apnsEnabled) {
                if (authManager.IsAuthenticated) {
                    RegisterDevice (SavedDeviceToken);
                } else {
                    UnregisterDevice (SavedDeviceToken);
                }
            } else if (authManager.IsAuthenticated) {
                RegisterForRemoteNotifications ();
            }
        }

        private static void IgnoreTaskErrors (Task task)
        {
            task.ContinueWith ((t) => {
                var e = t.Exception;
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Info (Tag, e, "Failed to send APNS info/action to server.");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void RegisterDevice (string token)
        {
            if (authToken != null) {
                var pushClient = ServiceContainer.Resolve<IPushClient> ();
                IgnoreTaskErrors (pushClient.Register (authToken, PushService.APNS, token));
            }
        }

        private void UnregisterDevice (string token)
        {
            if (!string.IsNullOrEmpty (SavedDeviceToken) && authToken != null) {
                var pushClient = ServiceContainer.Resolve<IPushClient> ();
                IgnoreTaskErrors (pushClient.Unregister (authToken, PushService.APNS, token));
            }
        }

        private void RegisterForRemoteNotifications()
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

        public void RegisteredForRemoteNotifications (UIApplication application, NSData deviceToken)
        {
            var DeviceToken = deviceToken.Description;
            if (!string.IsNullOrWhiteSpace (DeviceToken)) {
                DeviceToken = DeviceToken.Trim ('<').Trim ('>').Replace (" ", string.Empty);
            }

            var oldDeviceToken = NSUserDefaults.StandardUserDefaults.StringForKey (PushDeviceTokenKey);
            var oldDeviceTokenEmpty = string.IsNullOrEmpty (oldDeviceToken);

            NSUserDefaults.StandardUserDefaults.SetString (DeviceToken, PushDeviceTokenKey);

            if (oldDeviceTokenEmpty) {
                RegisterDevice (DeviceToken);
            } else if (!oldDeviceToken.Equals (DeviceToken)) {
                UnregisterDevice (oldDeviceToken);
            }
        }

        public void FailedToRegisterForRemoteNotifications (UIApplication application, NSError error)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            log.Info (Tag, "Failed To Register For Remote Notifications (APNS)");
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

        private static readonly NSString updatedAtConst = new NSString ("updated_at");
        private static readonly NSString taskIdConst = new NSString ("task_id");

        public async Task DidReceiveRemoteNotificationAsync (UIApplication application, NSDictionary userInfo, System.Action<UIBackgroundFetchResult> completionHandler)
        {
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
    }
}

