using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Gcm;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Data;

namespace Toggl.Joey.Net
{
    public class GcmRegistrationManager
    {
        private static readonly string Tag = "GcmRegistrationManager";
        private string authToken;
        #pragma warning disable 0414
        private readonly object subscriptionAuthChanged;
        #pragma warning restore 0414

        public GcmRegistrationManager ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            authToken = authManager.Token;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChangedMessage);

            CheckRegistrationId ();
        }

        private void CheckRegistrationId ()
        {
            if (!HasGcmSupport)
                return;

            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (!authManager.IsAuthenticated)
                return;

            if (RegistrationId == null) {
                // Registration ID missing (either due update or something else)
                RegisterDevice ();
            }
        }

        private bool HasGcmSupport {
            get {
                if (String.IsNullOrEmpty (Build.GcmSenderId))
                    return false;
                var ctx = ServiceContainer.Resolve<Context> ();
                return GooglePlayServicesUtil.IsGooglePlayServicesAvailable (ctx) == ConnectionResult.Success;
            }
        }

        private void OnAuthChangedMessage (AuthChangedMessage msg)
        {
            var hasGcm = HasGcmSupport;
            var authManager = ServiceContainer.Resolve<AuthManager> ();

            if (hasGcm) {
                UnregisterDevice ();
            }

            // Update cached data
            authToken = authManager.Token;

            if (authManager.IsAuthenticated) {
                if (hasGcm) {
                    RegisterDevice ();
                }
            }
        }

        private string RegistrationId {
            get {
                var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
                var id = settingsStore.GcmRegistrationId;

                if (id != null) {
                    var regVer = settingsStore.GcmAppVersion;
                    if (regVer != AppVersion) {
                        RegistrationId = id = null;
                    }
                }

                return id;
            }
            set {
                var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
                if (value == null) {
                    settingsStore.GcmRegistrationId = null;
                    settingsStore.GcmAppVersion = null;
                } else {
                    settingsStore.GcmRegistrationId = value;
                    settingsStore.GcmAppVersion = AppVersion;
                }
            }
        }

        private int AppVersion {
            get {
                var ctx = ServiceContainer.Resolve<Context> ();
                var info = ctx.PackageManager.GetPackageInfo (ctx.PackageName, 0);
                return info.VersionCode;
            }
        }

        private async void RegisterDevice ()
        {
            var regId = RegistrationId;

            if (regId == null) {
                // Obtain registration id for app:
                var ctx = ServiceContainer.Resolve<Context> ();
                var gcm = GoogleCloudMessaging.GetInstance (ctx);

                try {
                    RegistrationId = regId = await Task.Factory.StartNew (() =>
                        gcm.Register (Build.GcmSenderId));
                } catch (Exception exc) {
                    var log = ServiceContainer.Resolve<Logger> ();
                    log.Info (Tag, exc, "Failed register device for GCM push.");
                    return;
                }
            }

            // Register user device with server
            var pushClient = ServiceContainer.Resolve<IPushClient> ();
            IgnoreTaskErrors (pushClient.Register (authToken, PushService.GCM, regId));
        }

        private void UnregisterDevice ()
        {
            if (authToken == null)
                return;

            var regId = RegistrationId;
            if (regId != null) {
                var pushClient = ServiceContainer.Resolve<IPushClient> ();
                IgnoreTaskErrors (pushClient.Unregister (authToken, PushService.GCM, regId));
                RegistrationId = regId = null;
            }
        }

        private static void IgnoreTaskErrors (Task task)
        {
            task.ContinueWith ((t) => {
                var e = t.Exception;
                var log = ServiceContainer.Resolve<Logger> ();
                log.Info (Tag, e, "Failed to send GCM info to server.");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
