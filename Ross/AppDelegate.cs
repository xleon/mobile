using System;
using System.Collections.Generic;
using GoogleAnalytics.iOS;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Bugsnag;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Data;
using Toggl.Ross.Net;
using Toggl.Ross.ViewControllers;

namespace Toggl.Ross
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate, IPlatformInfo
    {
        private UIWindow window;
        private bool isResuming;

        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            RegisterComponents ();

            var signIn = Google.Plus.SignIn.SharedInstance;
            signIn.ClientId = Build.GoogleOAuthClientId;
            signIn.Scopes = new [] {
                "https://www.googleapis.com/auth/userinfo.profile",
                "https://www.googleapis.com/auth/userinfo.email",
            };

            Toggl.Ross.Theme.Style.Initialize ();

            // Start app
            window = new UIWindow (UIScreen.MainScreen.Bounds);
            window.RootViewController = new MainViewController ();
            window.MakeKeyAndVisible ();
            
            // Make sure critical services are running are running:
            ServiceContainer.Resolve<BugsnagClient> ();
            ServiceContainer.Resolve<BugsnagUserManager> ();

            #if DEBUG
            GAI.SharedInstance.DryRun = true;
            #endif
            ServiceContainer.Resolve<IGAITracker> ();

            return true;
        }

        public override void OnActivated (UIApplication application)
        {
            // Make sure the user data is refreshed when the application becomes active
            ServiceContainer.Resolve<ISyncManager> ().Run (SyncMode.Full);

            isResuming = true;
        }

        public override bool OpenUrl (UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            return Google.Plus.UrlHandler.HandleUrl (url, sourceApplication, annotation);
        }

        private void RegisterComponents ()
        {
            Services.Register ();

            // Override default implementation
            ServiceContainer.Register<ITimeProvider> (() => new NSTimeProvider ());

            // Register Ross components:
            ServiceContainer.Register<IPlatformInfo> (this);
            ServiceContainer.Register<SettingsStore> ();
            ServiceContainer.Register<ISettingsStore> (() => ServiceContainer.Resolve<SettingsStore> ());
            ServiceContainer.Register<BugsnagClient> (delegate {
                return new Toggl.Ross.Bugsnag.BugsnagClient (Build.BugsnagApiKey) {
                    DeviceId = ServiceContainer.Resolve<SettingsStore> ().InstallId,
                    ProjectNamespaces = new List<string> () { "Toggl." },
                    NotifyReleaseStages = new List<string> () { "production" },
                    #if DEBUG
                    ReleaseStage = "development",
                    #else
                    ReleaseStage = "production",
                    #endif
                };
            });
            ServiceContainer.Register<IGAITracker> (
                () => GAI.SharedInstance.GetTracker (Build.GoogleAnalyticsId));
            ServiceContainer.Register<INetworkPresence> (() => new NetworkPresence ());
        }

        string IPlatformInfo.AppIdentifier {
            get { return Build.AppIdentifier; }
        }

        private string appVersion;

        string IPlatformInfo.AppVersion {
            get {
                if (appVersion == null) {
                    appVersion = NSBundle.MainBundle.InfoDictionary.ObjectForKey (
                        new NSString ("CFBundleVersion")).ToString ();
                }
                return appVersion;
            }
        }
    }
}
