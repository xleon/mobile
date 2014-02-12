using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
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
        }

        private void RegisterComponents ()
        {
            // Register common Phoebe components:
            ServiceContainer.Register<MessageBus> ();
            ServiceContainer.Register<ModelManager> ();
            ServiceContainer.Register<AuthManager> ();
            ServiceContainer.Register<SyncManager> ();
            ServiceContainer.Register<ITogglClient> (() => new TogglRestClient (Build.ApiUrl));
            ServiceContainer.Register<IPushClient> (() => new PushRestClient (Build.ApiUrl));

            // Register Joey components:
            ServiceContainer.Register<Logger> (() => new AndroidLogger ());
            ServiceContainer.Register<Context> (this);
            ServiceContainer.Register<IPlatformInfo> (this);
            ServiceContainer.Register<SettingsStore> (() => new SettingsStore (Context));
            ServiceContainer.Register<ISettingsStore> (() => ServiceContainer.Resolve<SettingsStore> ());
            ServiceContainer.Register<IModelStore> (delegate {
                string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
                var path = System.IO.Path.Combine (folder, "toggl.db");
                return new SQLiteModelStore (path);
            });
            ServiceContainer.Register<GcmRegistrationManager> (new GcmRegistrationManager ());
            ServiceContainer.Register<AndroidNotificationManager> (new AndroidNotificationManager ());

            // Setup Bugsnag:
            ServiceContainer.Register<BugsnagClient> (
                new Toggl.Joey.Bugsnag.BugsnagClient (this, Build.BugsnagApiKey) {
                    DeviceId = ServiceContainer.Resolve<SettingsStore> ().InstallId,
                    ProjectNamespaces = new List<string> () { "Toggl." },
                });
            ServiceContainer.Register (new BugsnagUserManager ());
        }

        public override void OnTrimMemory (TrimMemory level)
        {
            base.OnTrimMemory (level);

            if (level <= TrimMemory.Moderate) {
                ServiceContainer.Resolve<IModelStore> ().Commit ();
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
    }
}
