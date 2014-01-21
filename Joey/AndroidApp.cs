using System;
using System.Linq;
using Android.App;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Data;

namespace Toggl.Joey
{
    [Application (
        Icon = "@drawable/Icon",
        Label = "@string/AppName",
        Description = "@string/AppDescription",
        Theme = "@style/Theme.App")]
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
            ServiceContainer.Register<AuthManager> ();
            ServiceContainer.Register<SyncManager> ();
            ServiceContainer.Register<ITogglClient> (delegate {
                #if DEBUG
                var url = new Uri ("https://next.toggl.com/api/");
                #else
                var url = new Uri("https://toggl.com/api/");
                #endif
                return new TogglRestClient (url);
            });

            // Register Joey components:
            ServiceContainer.Register<IPlatformInfo> (this);
            ServiceContainer.Register<SettingsStore> (() => new SettingsStore (Context));
            ServiceContainer.Register<ISettingsStore> (() => ServiceContainer.Resolve<SettingsStore> ());
            ServiceContainer.Register<IModelStore> (delegate {
                string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
                var path = System.IO.Path.Combine (folder, "toggl.db");
                return new SQLiteModelStore (path);
            });
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
            get { return "TogglJoey"; }
        }

        public string AppVersion {
            get { return PackageManager.GetPackageInfo (PackageName, 0).VersionName; }
        }
    }
}
