using System.Collections.Generic;
using Foundation;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using Toggl.Ross.Analytics;
using Toggl.Ross.Data;
using Toggl.Ross.Logging;
using Toggl.Ross.Net;
using Toggl.Ross.ViewControllers;
using Toggl.Ross.Views;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate, IPlatformInfo
    {
        private TogglWindow window;
        private int systemVersion;
        private const int minVersionWidget = 7;

        public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
        {
            var versionString = UIDevice.CurrentDevice.SystemVersion;
            systemVersion = System.Convert.ToInt32 ( versionString.Split ( new [] {"."}, System.StringSplitOptions.None)[0]);

            RegisterComponents ();

            var signIn = Google.Plus.SignIn.SharedInstance;
            signIn.ClientId = Build.GoogleOAuthClientId;
            signIn.Scopes = new [] {
                "https://www.googleapis.com/auth/userinfo.profile",
                "https://www.googleapis.com/auth/userinfo.email",
            };

            Toggl.Ross.Theme.Style.Initialize ();

            // Start app
            window = new TogglWindow (UIScreen.MainScreen.Bounds);
            window.RootViewController = new MainViewController ();
            window.MakeKeyAndVisible ();

            // Make sure critical services are running are running:
            ServiceContainer.Resolve<UpgradeManger> ().TryUpgrade ();
            ServiceContainer.Resolve<ILoggerClient> ();
            ServiceContainer.Resolve<LoggerUserManager> ();
            ServiceContainer.Resolve<ITracker> ();

            return true;
        }

        public override void OnActivated (UIApplication application)
        {
            // Make sure the user data is refreshed when the application becomes active
            ServiceContainer.Resolve<ISyncManager> ().Run ();
            ServiceContainer.Resolve<NetworkIndicatorManager> ();

            if (systemVersion > minVersionWidget) {
                ServiceContainer.Resolve<WidgetSyncManager>();
                var widgetService = ServiceContainer.Resolve<WidgetUpdateService>();
                widgetService.SetAppOnBackground (false);
            }
        }

        public override bool OpenUrl (UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            if (systemVersion > minVersionWidget) {
                if (url.AbsoluteString.Contains (WidgetUpdateService.TodayUrlPrefix)) {
                    var widgetManager = ServiceContainer.Resolve<WidgetSyncManager>();
                    if (url.AbsoluteString.Contains (WidgetUpdateService.StartEntryUrlPrefix)) {
                        widgetManager.StartStopTimeEntry();
                    } else {
                        widgetManager.ContinueTimeEntry();
                    }
                    return true;
                }
            }
            return Google.Plus.UrlHandler.HandleUrl (url, sourceApplication, annotation);
        }

        public override void DidEnterBackground (UIApplication application)
        {
            if (systemVersion > minVersionWidget) {
                var widgetService = ServiceContainer.Resolve<WidgetUpdateService>();
                widgetService.SetAppOnBackground (true);
            }
        }

        public override void WillTerminate (UIApplication application)
        {
            if (systemVersion > minVersionWidget) {
                var widgetService = ServiceContainer.Resolve<WidgetUpdateService>();
                widgetService.SetAppActivated (false);
            }
        }

        private void RegisterComponents ()
        {
            // Register platform info first.
            ServiceContainer.Register<IPlatformInfo> (this);

            Services.Register ();

            // Override default implementation
            ServiceContainer.Register<ITimeProvider> (() => new NSTimeProvider ());

            // Register Ross components:
            ServiceContainer.Register<ILogger> (() => new Logger ());
            ServiceContainer.Register<SettingsStore> ();
            ServiceContainer.Register<ISettingsStore> (() => ServiceContainer.Resolve<SettingsStore> ());
            if (systemVersion > minVersionWidget) {
                ServiceContainer.Register<WidgetUpdateService> (() => new WidgetUpdateService());
                ServiceContainer.Register<IWidgetUpdateService> (() => ServiceContainer.Resolve<WidgetUpdateService> ());
            }
            ServiceContainer.Register<ExperimentManager> (() => new ExperimentManager (
                typeof (Toggl.Phoebe.Analytics.Experiments),
                typeof (Toggl.Ross.Analytics.Experiments)));
            ServiceContainer.Register<ILoggerClient> (delegate {
                return new LogClient (Build.XamInsightsApiKey, Build.BugsnagApiKey) {
                    DeviceId = ServiceContainer.Resolve<SettingsStore> ().InstallId,
                    ProjectNamespaces = new List<string> () { "Toggl." },
                };
            });
            ServiceContainer.Register<ITracker> (() => new Tracker());
            ServiceContainer.Register<INetworkPresence> (() => new NetworkPresence ());
            ServiceContainer.Register<NetworkIndicatorManager> ();
            ServiceContainer.Register<TagChipCache> ();
        }

        public static TogglWindow TogglWindow
        {
            get {
                return ((AppDelegate)UIApplication.SharedApplication.Delegate).window;
            }
        }

        string IPlatformInfo.AppIdentifier
        {
            get { return Build.AppIdentifier; }
        }

        private string appVersion;

        string IPlatformInfo.AppVersion
        {
            get {
                if (appVersion == null) {
                    appVersion = NSBundle.MainBundle.InfoDictionary.ObjectForKey (
                                     new NSString ("CFBundleShortVersionString")).ToString ();
                }
                return appVersion;
            }
        }

        bool IPlatformInfo.IsWidgetAvailable
        {
            get {
                // iOS 8 is the version where Today Widgets are availables.
                return systemVersion > minVersionWidget;
            }
        }
    }
}
