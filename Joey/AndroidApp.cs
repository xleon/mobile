using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Net;
using Google.Analytics.Tracking;
using Toggl.Phoebe;
using Toggl.Phoebe.Bugsnag;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Data;
using Toggl.Joey.Net;

namespace Toggl.Joey
{
    [Application (
        Icon = "@drawable/Icon",
        Label = "@string/AppName",
        Description = "@string/AppDescription",
        Theme = "@style/Theme.Toggl.App")]
    [MetaData ("com.google.android.gms.version",
        Value = "@integer/google_play_services_version")]
    class AndroidApp : Application, IPlatformInfo
    {
        bool componentsInitialized = false;

        public AndroidApp () : base ()
        {
        }

        public AndroidApp (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer) : base (javaRef, transfer)
        {
        }

        public override void OnCreate ()
        {
            base.OnCreate ();

            RegisterComponents ();
            InitializeStartupComponents ();
        }

        private void RegisterComponents ()
        {
            Services.Register ();

            // Register Joey components:
            ServiceContainer.Register<Logger> (() => new AndroidLogger ());
            ServiceContainer.Register<Context> (this);
            ServiceContainer.Register<IPlatformInfo> (this);
            ServiceContainer.Register<SettingsStore> (() => new SettingsStore (Context));
            ServiceContainer.Register<ISettingsStore> (() => ServiceContainer.Resolve<SettingsStore> ());
            ServiceContainer.Register<SyncMonitor> ();
            ServiceContainer.Register<GcmRegistrationManager> ();
            ServiceContainer.Register<AndroidNotificationManager> ();
            ServiceContainer.Register<BugsnagClient> (delegate {
                return new Toggl.Joey.Bugsnag.BugsnagClient (this, Build.BugsnagApiKey) {
                    DeviceId = ServiceContainer.Resolve<SettingsStore> ().InstallId,
                    ProjectNamespaces = new List<string> () { "Toggl." },
                };
            });
            ServiceContainer.Register<EasyTracker> (delegate {
                #if DEBUG
                GoogleAnalytics.GetInstance (this).SetDryRun (true);
                #endif

                var tracker = EasyTracker.GetInstance (this);
                tracker.Set (Fields.TrackingId, Build.GoogleAnalyticsId);
                return tracker;
            });
            ServiceContainer.Register<INetworkPresence> (() => new NetworkPresence (Context, (ConnectivityManager)GetSystemService (ConnectivityService)));
        }

        private void InitializeStartupComponents ()
        {
            ServiceContainer.Resolve<BugsnagClient> ();
            ServiceContainer.Resolve<BugsnagUserManager> ();
        }

        public void InitializeComponents ()
        {
            if (componentsInitialized)
                return;

            componentsInitialized = true;
            ServiceContainer.Resolve<SyncMonitor> ();
            ServiceContainer.Resolve<GcmRegistrationManager> ();
            ServiceContainer.Resolve<AndroidNotificationManager> ();
        }

        public override void OnTrimMemory (TrimMemory level)
        {
            base.OnTrimMemory (level);

            if (level <= TrimMemory.Moderate) {
                if (level <= TrimMemory.Complete) {
                    System.GC.Collect (GC.MaxGeneration);
                } else {
                    System.GC.Collect ();
                }
            }
        }

        public string AppIdentifier {
            get { return Build.AppIdentifier; }
        }

        public string AppVersion {
            get { return PackageManager.GetPackageInfo (PackageName, 0).VersionName; }
        }

        public bool ComponentsInitialized {
            get { return componentsInitialized; }
        }
    }
}
