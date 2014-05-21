using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Newtonsoft.Json;
using Toggl.Phoebe.Bugsnag.Data;
using Toggl.Phoebe.Bugsnag.IO;

namespace Toggl.Ross.Bugsnag
{
    public class BugsnagClient : Toggl.Phoebe.Bugsnag.BugsnagClient
    {
        private static readonly TimeSpan IdleTimeForSessionEnd = TimeSpan.FromSeconds (10);
        private static readonly string Tag = "Bugsnag";
        private readonly bool sendMetrics;
        private readonly string errorsCachePath;
        private DateTime appStartTime;
        private DateTime sessionPauseTime;
        private DateTime sessionStartTime;
        private bool storeOnly;
        private bool isInitialised;
        private ApplicationInfo appInfo;
        private SystemInfo systemInfo;
        private DateTime lastMemoryWarning;
        private bool inForeground;
        private float batteryLevel;
        private bool isCharging;
        private string orientation;
        private NSObject notifApplicationDidBecomeActive;
        private NSObject notifAapplicationDidEnterBackground;
        private NSObject notifDeviceBatteryStateDidChange;
        private NSObject notifDeviceBatteryLevelDidChange;
        private NSObject notifDeviceOrientationDidChange;
        private NSObject notifApplicationDidReceiveMemoryWarning;

        public BugsnagClient (string apiKey, bool enableMetrics = true) : base (apiKey)
        {
            sendMetrics = enableMetrics;
            appStartTime = DateTime.UtcNow;
            errorsCachePath = MakeCachePath ();

            // TODO: Install crash handlers

            // Register observers
            notifApplicationDidBecomeActive = NSNotificationCenter.DefaultCenter.AddObserver (
                UIApplication.DidBecomeActiveNotification, OnApplicationDidBecomeActive);
            notifAapplicationDidEnterBackground = NSNotificationCenter.DefaultCenter.AddObserver (
                UIApplication.DidEnterBackgroundNotification, OnApplicationDidEnterBackground);
            notifApplicationDidReceiveMemoryWarning = NSNotificationCenter.DefaultCenter.AddObserver (
                UIApplication.DidReceiveMemoryWarningNotification, OnApplicationDidReceiveMemoryWarning);
            notifDeviceBatteryStateDidChange = NSNotificationCenter.DefaultCenter.AddObserver (
                UIDevice.BatteryStateDidChangeNotification, OnBatteryChanged);
            notifDeviceBatteryLevelDidChange = NSNotificationCenter.DefaultCenter.AddObserver (
                UIDevice.BatteryLevelDidChangeNotification, OnBatteryChanged);
            notifDeviceOrientationDidChange = NSNotificationCenter.DefaultCenter.AddObserver (
                UIDevice.OrientationDidChangeNotification, OnOrientationChanged);

            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;
            UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications ();
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                // Remove observers
                if (notifApplicationDidBecomeActive != null) {
                    NSNotificationCenter.DefaultCenter.RemoveObserver (notifApplicationDidBecomeActive);
                    notifApplicationDidBecomeActive = null;
                }
                if (notifAapplicationDidEnterBackground != null) {
                    NSNotificationCenter.DefaultCenter.RemoveObserver (notifAapplicationDidEnterBackground);
                    notifAapplicationDidEnterBackground = null;
                }
                if (notifApplicationDidReceiveMemoryWarning != null) {
                    NSNotificationCenter.DefaultCenter.RemoveObserver (notifApplicationDidReceiveMemoryWarning);
                    notifApplicationDidReceiveMemoryWarning = null;
                }
                if (notifDeviceBatteryStateDidChange != null) {
                    NSNotificationCenter.DefaultCenter.RemoveObserver (notifDeviceBatteryStateDidChange);
                    notifDeviceBatteryStateDidChange = null;
                }
                if (notifDeviceBatteryLevelDidChange != null) {
                    NSNotificationCenter.DefaultCenter.RemoveObserver (notifDeviceBatteryLevelDidChange);
                    notifDeviceBatteryLevelDidChange = null;
                }
                if (notifDeviceOrientationDidChange != null) {
                    NSNotificationCenter.DefaultCenter.RemoveObserver (notifDeviceOrientationDidChange);
                    notifDeviceOrientationDidChange = null;
                }

                // TODO: Uninstall crash handlers
            }

            base.Dispose (disposing);
        }

        public string DeviceId { get; set; }

        private void OnApplicationDidBecomeActive (NSNotification notif)
        {
            inForeground = true;

            if (DateTime.UtcNow - sessionPauseTime > IdleTimeForSessionEnd) {
                sessionStartTime = DateTime.UtcNow;
            }

            if (!isInitialised) {
                if (sendMetrics) {
                    TrackUser ();
                }
                FlushReports ();
                isInitialised = true;
            }
        }

        private void OnApplicationDidEnterBackground (NSNotification notif)
        {
            inForeground = false;
            sessionPauseTime = DateTime.UtcNow;
        }

        private void OnApplicationDidReceiveMemoryWarning (NSNotification notif)
        {
            lastMemoryWarning = DateTime.UtcNow;
        }

        private void OnBatteryChanged (NSNotification notif)
        {
            batteryLevel = UIDevice.CurrentDevice.BatteryLevel;
            isCharging = UIDevice.CurrentDevice.BatteryState == UIDeviceBatteryState.Charging;
        }

        private void OnOrientationChanged (NSNotification notif)
        {
            switch (UIDevice.CurrentDevice.Orientation) {
            case UIDeviceOrientation.PortraitUpsideDown:
                orientation = "portraitupsidedown";
                break;
            case UIDeviceOrientation.Portrait:
                orientation = "portrait";
                break;
            case UIDeviceOrientation.LandscapeRight:
                orientation = "landscaperight";
                break;
            case UIDeviceOrientation.LandscapeLeft:
                orientation = "landscapeleft";
                break;
            case UIDeviceOrientation.FaceUp:
                orientation = "faceup";
                break;
            case UIDeviceOrientation.FaceDown:
                orientation = "facedown";
                break;
            case UIDeviceOrientation.Unknown:
            default:
                orientation = "unknown";
                break;
            }
        }

        protected override void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating) {
                // At this point in time we don't want to attempt an HTTP connection, thus we only store
                // the event to disk and hope that the user opens the application again to send the
                // errors to Bugsnag.
                storeOnly = true;
            }
            base.OnUnhandledException (sender, e);
        }

        private string notifPrepend = null;
        private string notifAppend = null;

        private Stream MakeNotification (Stream[] jsonEventStreams)
        {
            if (notifPrepend == null || notifAppend == null) {
                var json = JsonConvert.SerializeObject (new Notification () {
                    ApiKey = ApiKey,
                    Notifier = Notifier,
                    Events = new List<Event> (0),
                });

                // Find empty events array:
                var idx = json.IndexOf ("[]");
                notifPrepend = json.Substring (0, idx + 1);
                notifAppend = json.Substring (idx + 1);
            }

            var stream = new CombiningStream ();
            stream.Add (notifPrepend);
            if (jsonEventStreams.Length > 1) {
                var eventsStream = new CombiningStream (", ");
                foreach (var eventStream in jsonEventStreams) {
                    eventsStream.Add (eventStream);
                }
                stream.Add (eventsStream);
            } else if (jsonEventStreams.Length == 1) {
                stream.Add (jsonEventStreams [0]);
            }
            stream.Add (notifAppend);

            return stream;
        }

        private Stream TryStoreEvent (Event e, string path)
        {
            var json = new MemoryStream (
                           System.Text.Encoding.UTF8.GetBytes (
                               JsonConvert.SerializeObject (e)));

            // Don't even try storing to disk when invalid path
            if (path == null)
                return json;

            FileStream output = null;
            try {
                output = new FileStream (path, FileMode.CreateNew);
                json.CopyTo (output);
                output.Flush ();

                output.Seek (0, SeekOrigin.Begin);
                json.Dispose ();
                return output;
            } catch (IOException ex) {
                LogError (String.Format ("Failed to store error to disk: {0}", ex));

                // Failed to store to disk (full?), return json memory stream instead
                if (output != null) {
                    output.Dispose ();
                }
                json.Seek (0, SeekOrigin.Begin);
                return json;
            }
        }

        protected override void SendEvent (Event e)
        {
            // Determine file where to persist the error:
            string path = null;
            if (errorsCachePath != null) {
                var file = String.Format ("{0}.json", DateTime.UtcNow.ToBinary ());
                path = Path.Combine (errorsCachePath, file);
            }

            Stream eventStream = null;
            Stream notifStream = null;
            try {
                // Serialize the event:
                eventStream = TryStoreEvent (e, path);
                if (storeOnly) {
                    storeOnly = false;
                    return;
                }

                // Combine into a valid payload:
                notifStream = MakeNotification (new Stream[] { eventStream });

                SendNotification (notifStream).ContinueWith ((t) => {
                    try {
                        if (t.Result) {
                            // On successful response delete the stored file:
                            try {
                                File.Delete (path);
                            } catch (Exception ex) {
                                LogError (String.Format ("Failed to clean up stored event: {0}", ex));
                            }
                        }
                    } finally {
                        if (notifStream != null) {
                            // Also disposes of the eventStream
                            notifStream.Dispose ();
                        }
                    }
                });
            } catch (Exception ex) {
                // Something went wrong...
                LogError (String.Format ("Failed to send notification: {0}", ex));

                if (notifStream != null) {
                    // Also disposes of the eventStream
                    notifStream.Dispose ();
                } else if (eventStream != null) {
                    eventStream.Dispose ();
                }
            }
        }

        private void FlushReports ()
        {
            if (errorsCachePath == null)
                return;

            var files = Directory.GetFiles (errorsCachePath);
            if (files.Length == 0)
                return;

            var streams = new List<Stream> (files.Length);
            foreach (var path in files) {
                try {
                    streams.Add (new FileStream (path, FileMode.Open));
                } catch (Exception ex) {
                    LogError (String.Format ("Failed to open cached file {0}: {1}", Path.GetFileName (path), ex));
                }
            }

            Stream notifStream = null;
            try {
                // Make a single request to send all stored events
                notifStream = MakeNotification (streams.ToArray ());

                SendNotification (notifStream).ContinueWith ((t) => {
                    try {
                        // Remove cached files on success
                        if (t.Result) {
                            foreach (var path in files) {
                                try {
                                    File.Delete (path);
                                } catch (Exception ex) {
                                    LogError (String.Format ("Failed to clean up stored event {0}: {1}",
                                        Path.GetFileName (path), ex));
                                }
                            }
                        }
                    } finally {
                        if (notifStream != null) {
                            notifStream.Dispose ();
                        }
                    }
                });
            } catch (Exception ex) {
                // Something went wrong...
                LogError (String.Format ("Failed to send notification: {0}", ex));

                if (notifStream != null) {
                    // Notification stream closes all other streams:
                    notifStream.Dispose ();
                } else {
                    foreach (var stream in streams) {
                        stream.Dispose ();
                    }
                }
                streams.Clear ();
            }
        }

        private Task<bool> SendNotification (Stream stream)
        {
            var req = new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = BaseUrl,
                Content = new StreamContent (stream),
            };
            req.Content.Headers.ContentType = new MediaTypeHeaderValue ("application/json");

            return HttpClient.SendAsync (req).ContinueWith ((t) => {
                try {
                    var resp = t.Result;
                    if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                        LogError ("Failed to send notification due to invalid API key.");
                    } else if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest) {
                        LogError ("Failed to send notification due to invalid payload.");
                    } else {
                        return true;
                    }
                } catch (Exception ex) {
                    // Keep the stored file, it will be retried on next app start
                    LogError (String.Format ("Failed to send notification: {0}", ex));
                }

                return false;
            });
        }

        protected override void LogError (string msg)
        {
            Console.WriteLine ("[{0}] {1}", Tag, msg);
        }

        protected override UserInfo GetUserInfo ()
        {
            var val = base.GetUserInfo ();
            if (String.IsNullOrEmpty (val.Id)) {
                val.Id = DeviceId;
            }
            return val;
        }

        protected override ApplicationInfo GetAppInfo ()
        {
            if (appInfo == null) {
                var bundle = NSBundle.MainBundle;
                var version = (string)(NSString)bundle.ObjectForInfoDictionary ("CFBundleShortVersionString");
                var bundleVersion = (string)(NSString)bundle.ObjectForInfoDictionary ("CFBundleVersion");
                var name = (string)(NSString)bundle.ObjectForInfoDictionary ("CFBundleDisplayName");

                appInfo = new Toggl.Ross.Bugsnag.Data.ApplicationInfo () {
                    Id = bundle.BundleIdentifier,
                    Version = version,
                    BundleVersion = bundleVersion,
                    Name = name,
                    ReleaseStage = ReleaseStage,
                };
            }
            return appInfo;
        }

        protected override ApplicationState GetAppState ()
        {
            return new Toggl.Ross.Bugsnag.Data.ApplicationState () {
                SessionLength = DateTime.UtcNow - sessionStartTime,
                TimeSinceMemoryWarning = DateTime.UtcNow - lastMemoryWarning,
                InForeground = inForeground,
                CurrentScreen = AppleInfo.TopMostViewController,
                RunningTime = DateTime.UtcNow - appStartTime,
                // TODO: Implement memory usage recording
            };
        }

        protected override SystemInfo GetSystemInfo ()
        {
            if (systemInfo == null) {
                systemInfo = new Toggl.Ross.Bugsnag.Data.SystemInfo () {
                    Id = DeviceId,
                    Manufacturer = "Apple",
                    Model = AppleInfo.Model,
                    ScreenDensity = UIScreen.MainScreen.Scale,
                    ScreenResolution = AppleInfo.ScreenResolution,
                    TotalMemory = AppleInfo.TotalMemory,
                    OperatingSystem = "iOS",
                    OperatingSystemVersion = NSProcessInfo.ProcessInfo.OperatingSystemVersionString,
                    IsJailbroken = UIApplication.SharedApplication.CanOpenUrl (new NSUrl ("cydia://")),
                    Locale = NSLocale.CurrentLocale.LocaleIdentifier,
                    DiskSize = AppleInfo.FileSystemAttributes.Size,
                };
            }
            return systemInfo;
        }

        protected override SystemState GetSystemState ()
        {
            return new Toggl.Ross.Bugsnag.Data.SystemState () {
                FreeMemory = AppleInfo.FreeMemory,
                Orientation = orientation,
                BatteryLevel = batteryLevel,
                IsCharging = isCharging,
                AvailableDiskSpace = AppleInfo.FileSystemAttributes.FreeSize,
                // TODO: Implement LocationStatus reporting
                // TODO: Implement NetworkStatus reporting
            };
        }

        private static string MakeCachePath ()
        {
            var path = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "bugsnag-events");
            if (!Directory.Exists (path)) {
                try {
                    Directory.CreateDirectory (path);
                } catch (Exception ex) {
                    Console.WriteLine ("[{0}] Failed to create cache dir: {1}", Tag, ex);
                    path = null;
                }
            }

            return path;
        }

        public static class AppleInfo
        {
            public static NSFileSystemAttributes FileSystemAttributes {
                get {
                    var paths = NSSearchPath.GetDirectories (NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User, true);
                    return NSFileManager.DefaultManager.GetFileSystemAttributes (paths.Last ());
                }
            }

            public static string Model {
                get {
                    long size = 0;
                    if (sysctlbyname ("hw.machine", null, ref size, IntPtr.Zero, 0) == 0) {
                        var buf = new byte[size];
                        if (sysctlbyname ("hw.machine", buf, ref size, IntPtr.Zero, 0) == 0) {
                            return Encoding.UTF8.GetString (buf, 0, (int)size);
                        }
                    }
                    return null;
                }
            }

            public static ulong TotalMemory {
                get {
                    var buf = new byte[sizeof(ulong)];
                    var size = buf.LongLength;
                    if (sysctlbyname ("hw.memsize", buf, ref size, IntPtr.Zero, 0) == 0) {
                        return BitConverter.ToUInt64 (buf, 0);
                    }
                    return 0;
                }
            }

            public static ulong FreeMemory {
                get {
                    ulong pageSize;
                    ulong pagesFree;

                    var buf = new byte[sizeof(ulong)];
                    var size = buf.LongLength;
                    if (sysctlbyname ("vm.page_free_count", buf, ref size, IntPtr.Zero, 0) == 0) {
                        pagesFree = BitConverter.ToUInt64 (buf, 0);
                        if (sysctlbyname ("hw.pagesize", buf, ref size, IntPtr.Zero, 0) == 0) {
                            pageSize = BitConverter.ToUInt64 (buf, 0);
                            return pagesFree * pageSize;
                        }
                    }
                    return 0;
                }
            }

            public static string ScreenResolution {
                get {
                    var size = UIScreen.MainScreen.Bounds.Size;
                    var scale = UIScreen.MainScreen.Scale;
                    return String.Format ("{0}x{1}", (int)(size.Width * scale), (int)(size.Height * scale)); 
                }
            }

            [DllImport (MonoTouch.Constants.SystemLibrary)]
            static internal extern int sysctlbyname ([MarshalAs (UnmanagedType.LPStr)] string property, byte[] output, ref long oldLen, IntPtr newp, uint newlen);

            public static string TopMostViewController {
                get {
                    UIViewController viewController = UIApplication.SharedApplication.KeyWindow.RootViewController;

                    if (viewController is UINavigationController) {
                        viewController = ((UINavigationController)viewController).VisibleViewController;
                    }

                    var depth = 0;

                    while (viewController != null && depth <= 30) {
                        var presentedController = viewController.PresentedViewController;

                        if (presentedController == null) {
                            return viewController.GetType ().ToString ();
                        } else if (presentedController is UINavigationController) {
                            viewController = ((UINavigationController)presentedController).VisibleViewController;
                        } else {
                            viewController = presentedController;
                        }

                        depth++;
                    }

                    return viewController != null ? viewController.GetType ().ToString () : null;
                }
            }
        }
    }
}
