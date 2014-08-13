using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Android.Content;
using Android.Util;
using Newtonsoft.Json;
using Toggl.Phoebe.Bugsnag.Data;
using Toggl.Phoebe.Bugsnag.IO;

namespace Toggl.Joey.Bugsnag
{
    public class BugsnagClient : Toggl.Phoebe.Bugsnag.BugsnagClient
    {
        private static readonly TimeSpan IdleTimeForSessionEnd = TimeSpan.FromSeconds (10);
        private static readonly TimeSpan StateCacheTimeToLive = TimeSpan.FromSeconds (1);
        private static readonly string Tag = "Bugsnag";
        private readonly Context androidContext;
        private readonly List<WeakReference> activityStack = new List<WeakReference> ();
        private readonly bool sendMetrics;
        private readonly string errorsCachePath;
        private bool isInitialised;
        private WeakReference topActivity;
        private DateTime appStartTime;
        private DateTime sessionPauseTime;
        private DateTime sessionStartTime;
        private SystemInfo systemInfo;
        private ApplicationInfo appInfo;
        private DateTime? cachedStateTime;
        private ApplicationState cachedAppState;
        private SystemState cachedSystemState;
        private bool storeOnly;

        public BugsnagClient (Context context, string apiKey, bool enableMetrics = true) : base (apiKey)
        {
            sendMetrics = enableMetrics;
            androidContext = context.ApplicationContext;
            appStartTime = DateTime.UtcNow;
            errorsCachePath = MakeCachePath (context);
            GuessReleaseStage ();

            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += OnUnhandledManagedException;
            JavaExceptionHandler.Install (this);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser -= OnUnhandledManagedException;
                JavaExceptionHandler.CleanUp ();
            }

            base.Dispose (disposing);
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

        private void OnUnhandledManagedException (object sender, Android.Runtime.RaiseThrowableEventArgs e)
        {
            if (!AutoNotify)
                return;

            // Cache the app and system states, since filling them when the exception bubles to the global
            // app domain unhandled exception is impossible.
            CacheStates ();
        }

        public string DeviceId { get; set; }

        public void OnActivityCreated (Context ctx)
        {
            activityStack.Add (new WeakReference (ctx));
        }

        public void OnActivityResumed (Context ctx)
        {
            topActivity = new WeakReference (ctx);
            Context = TopActivityName;

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

        public void OnActivityPaused (Context ctx)
        {
            topActivity = null;
            Context = null;
            sessionPauseTime = DateTime.UtcNow;
        }

        public void OnActivityDestroyed (Context ctx)
        {
            activityStack.RemoveAll ((w) => !w.IsAlive || w.Target == ctx);
        }

        private void GuessReleaseStage ()
        {
            bool debuggable = false;
            try {
                var app = androidContext.PackageManager.GetApplicationInfo (androidContext.PackageName, 0);
                debuggable = (app.Flags & Android.Content.PM.ApplicationInfoFlags.Debuggable) != 0;
            } catch (Java.Lang.Throwable ex) {
                Log.Warn (Tag, ex, "Failed automatic release stage detection.");
            }
            ReleaseStage = debuggable ? "development" : "production";
        }

        private bool InForeground {
            get { return topActivity != null; }
        }

        public string TopActivityName {
            get {
                if (topActivity == null)
                    return null;

                var ctx = topActivity.Target as Context;
                if (ctx == null)
                    return null;

                return ctx.GetType ().Name;
            }
        }

        public TimeSpan SessionLength {
            get {
                if (InForeground) {
                    return DateTime.UtcNow - sessionStartTime;
                } else {
                    return TimeSpan.Zero;
                }
            }
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
            var httpClient = MakeHttpClient ();
            var req = new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = BaseUrl,
                Content = new StreamContent (stream),
            };
            req.Content.Headers.ContentType = new MediaTypeHeaderValue ("application/json");

            return httpClient.SendAsync (req).ContinueWith ((t) => {
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
                } finally {
                    httpClient.Dispose ();
                }

                return false;
            });
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
                appInfo = new Toggl.Joey.Bugsnag.Data.ApplicationInfo () {
                    Id = androidContext.PackageName,
                    Package = androidContext.PackageName,
                    Version = AndroidInfo.GetAppVersion (androidContext),
                    Name = AndroidInfo.GetAppName (androidContext),
                    ReleaseStage = ReleaseStage,
                };
            }
            return appInfo;
        }

        private void CacheStates ()
        {
            // Old cache still valid
            if (cachedStateTime.HasValue && cachedStateTime.Value + StateCacheTimeToLive > DateTime.UtcNow)
                return;

            cachedStateTime = null;
            GetAppInfo ();
            cachedAppState = GetAppState ();
            GetSystemInfo ();
            cachedSystemState = GetSystemState ();
            cachedStateTime = DateTime.UtcNow;
        }

        protected override ApplicationState GetAppState ()
        {
            if (cachedStateTime.HasValue && cachedAppState != null) {
                if (cachedStateTime.Value + StateCacheTimeToLive > DateTime.UtcNow) {
                    return cachedAppState;
                } else {
                    cachedStateTime = null;
                }
            }

            return new Toggl.Joey.Bugsnag.Data.ApplicationState () {
                SessionLength = SessionLength,
                HasLowMemory = AndroidInfo.CheckMemoryLow (androidContext),
                InForeground = InForeground,
                ActivityStack = activityStack.Select ((w) => w.Target as Context)
                    .Where ((ctx) => ctx != null)
                    .Select ((ctx) => ctx.GetType ().Name)
                    .ToList (),
                CurrentActivity = TopActivityName,
                RunningTime = DateTime.UtcNow - appStartTime,
                MemoryUsage = AndroidInfo.GetMemoryUsedByApp (),
            };
        }

        protected override SystemInfo GetSystemInfo ()
        {
            if (systemInfo == null) {
                systemInfo = new Toggl.Joey.Bugsnag.Data.SystemInfo () {
                    Id = DeviceId,
                    Manufacturer = Android.OS.Build.Manufacturer,
                    Model = Android.OS.Build.Model,
                    ScreenDensity = androidContext.Resources.DisplayMetrics.Density,
                    ScreenResolution = AndroidInfo.GetScreenResolution (androidContext),
                    TotalMemory = (ulong)AndroidInfo.GetMemoryAvailable (),
                    OperatingSystem = "android",
                    OperatingSystemVersion = Android.OS.Build.VERSION.Release,
                    ApiLevel = (int)Android.OS.Build.VERSION.SdkInt,
                    IsRooted = AndroidInfo.CheckRoot (),
                    Locale = Java.Util.Locale.Default.ToString (),
                };
            }
            return systemInfo;
        }

        protected override SystemState GetSystemState ()
        {
            if (cachedStateTime.HasValue && cachedSystemState != null) {
                if (cachedStateTime.Value + StateCacheTimeToLive > DateTime.UtcNow) {
                    return cachedSystemState;
                } else {
                    cachedStateTime = null;
                }
            }

            return new Toggl.Joey.Bugsnag.Data.SystemState () {
                FreeMemory = (ulong)AndroidInfo.GetFreeMemory (),
                Orientation = AndroidInfo.GetOrientation (androidContext),
                BatteryLevel = AndroidInfo.GetBatteryLevel (androidContext),
                IsCharging = AndroidInfo.CheckBatteryCharging (androidContext),
                AvailableDiskSpace = (ulong)AndroidInfo.GetAvailableDiskSpace (),
                LocationStatus = AndroidInfo.GetGpsStatus (androidContext),
                NetworkStatus = AndroidInfo.GetNetworkStatus (androidContext),
            };
        }

        protected override ExceptionInfo ConvertException (Exception ex)
        {
            var t = ex as Java.Lang.Throwable;
            if (t != null) {
                return ConvertThrowable (t);
            }
            return base.ConvertException (ex);
        }

        private ExceptionInfo ConvertThrowable (Java.Lang.Throwable ex)
        {
            var type = ex.GetType ();

            return new ExceptionInfo () {
                Name = type.Name,
                Message = ex.LocalizedMessage,
                Stack = ex.GetStackTrace ().Select ((frame) => new StackInfo () {
                    Method = String.Format ("{0}:{1}", frame.ClassName, frame.MethodName),
                    File = frame.FileName ?? "Unknown",
                    Line = frame.LineNumber,
                    InProject = IsInProject (frame.ClassName),
                }).ToList (),
            };
        }

        private bool IsInProject (string javaName)
        {
            var namespaces = ProjectNamespaces;
            if (namespaces == null)
                return false;

            return namespaces.Any ((ns) => javaName.StartsWith (ns));
        }

        protected override void LogError (string msg)
        {
            Log.Error (Tag, msg);
        }

        private static string MakeCachePath (Context ctx)
        {
            var path = Path.Combine (ctx.CacheDir.AbsolutePath, "bugsnag-events");
            if (!Directory.Exists (path)) {
                try {
                    Directory.CreateDirectory (path);
                } catch (Exception ex) {
                    Log.Error (Tag, String.Format ("Failed to create cache dir: {0}", ex));
                    path = null;
                }
            }

            return path;
        }

        private class JavaExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
        {
            private readonly BugsnagClient client;
            private readonly Java.Lang.Thread.IUncaughtExceptionHandler nextHandler;

            private JavaExceptionHandler (BugsnagClient client, Java.Lang.Thread.IUncaughtExceptionHandler original)
            {
                this.client = client;
                this.nextHandler = original;
            }

            public void UncaughtException (Java.Lang.Thread thread, Java.Lang.Throwable ex)
            {
                if (client.AutoNotify) {
                    client.Notify (ex, ErrorSeverity.Fatal);
                }
                if (nextHandler != null) {
                    nextHandler.UncaughtException (thread, ex);
                }
            }

            public static void Install (BugsnagClient client)
            {
                var current = Java.Lang.Thread.DefaultUncaughtExceptionHandler;
                var self = current as JavaExceptionHandler;
                if (self != null) {
                    current = self.nextHandler;
                }

                Java.Lang.Thread.DefaultUncaughtExceptionHandler = new JavaExceptionHandler (client, current);
            }

            public static void CleanUp ()
            {
                var current = Java.Lang.Thread.DefaultUncaughtExceptionHandler as JavaExceptionHandler;
                if (current != null) {
                    Java.Lang.Thread.DefaultUncaughtExceptionHandler = current.nextHandler;
                }
            }
        }

        private static class AndroidInfo
        {
            public static long GetMemoryUsedByApp ()
            {
                try {
                    var rt = Java.Lang.Runtime.GetRuntime ();
                    return rt.TotalMemory () - rt.FreeMemory ();
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to calculate memory used by the application.");
                    return 0;
                }
            }

            public static string GetScreenResolution (Context ctx)
            {
                try {
                    var dm = ctx.Resources.DisplayMetrics;
                    return String.Format ("{0}x{1}",
                        Math.Max (dm.WidthPixels, dm.HeightPixels),
                        Math.Min (dm.WidthPixels, dm.HeightPixels));
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to get screen resolution.");
                    return null;
                }
            }

            public static long GetMemoryAvailable ()
            {
                try {
                    var rt = Java.Lang.Runtime.GetRuntime ();
                    if (rt.MaxMemory () != long.MaxValue) {
                        return rt.MaxMemory ();
                    } else {
                        return rt.TotalMemory ();
                    }
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to retrieve available memory amount.");
                    return 0;
                }
            }

            public static long GetFreeMemory ()
            {
                return GetMemoryAvailable () - GetMemoryUsedByApp ();
            }

            public static bool CheckRoot ()
            {
                return CheckTestKeysBuild () || CheckSuperUserAPK ();
            }

            private static bool CheckTestKeysBuild ()
            {
                var tags = Android.OS.Build.Tags;
                return tags != null && tags.Contains ("test-keys");
            }

            private static bool CheckSuperUserAPK ()
            {
                try {
                    return System.IO.File.Exists ("/system/app/Superuser.apk");
                } catch {
                    return false;
                }
            }

            public static Android.Content.Res.Orientation GetOrientation (Context ctx)
            {
                try {
                    return ctx.Resources.Configuration.Orientation;
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to determine device orientation.");
                    return Android.Content.Res.Orientation.Undefined;
                }
            }

            public static bool CheckBatteryCharging (Context ctx)
            {
                try {
                    var filter = new IntentFilter (Intent.ActionBatteryChanged);
                    var intent = ctx.RegisterReceiver (null, filter);

                    var status = (Android.OS.BatteryStatus)intent.GetIntExtra ("status", -1);
                    return status == Android.OS.BatteryStatus.Charging || status == Android.OS.BatteryStatus.Full;
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to determine if the battery is charging.");
                    return false;
                }
            }

            public static float GetBatteryLevel (Context ctx)
            {
                try {
                    var filter = new IntentFilter (Intent.ActionBatteryChanged);
                    var intent = ctx.RegisterReceiver (null, filter);

                    int level = intent.GetIntExtra ("level", -1);
                    int scale = intent.GetIntExtra ("scale", -1);

                    return level / (float)scale;
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to determine battery level.");
                    return 0;
                }
            }

            public static long GetAvailableDiskSpace ()
            {
                try {
                    var externalStat = new Android.OS.StatFs (Android.OS.Environment.ExternalStorageDirectory.Path);
                    var externalAvail = (long)externalStat.BlockSize * (long)externalStat.BlockCount;

                    var internalStat = new Android.OS.StatFs (Android.OS.Environment.DataDirectory.Path);
                    var internalAvail = (long)internalStat.BlockSize * (long)internalStat.BlockCount;

                    return Math.Min (externalAvail, internalAvail);
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to determine available disk space.");
                    return 0;
                }
            }

            public static string GetGpsStatus (Context ctx)
            {
                try {
                    var cr = ctx.ContentResolver;
                    var providers = Android.Provider.Settings.Secure.GetString (
                                        cr, Android.Provider.Settings.Secure.LocationProvidersAllowed);
                    if (providers != null && providers.Length > 0) {
                        return "allowed";
                    } else {
                        return "disallowed";
                    }
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to determine GPS status.");
                    return null;
                }
            }

            public static string GetNetworkStatus (Context ctx)
            {
                try {
                    var cm = (Android.Net.ConnectivityManager)ctx.GetSystemService (
                                 Android.Content.Context.ConnectivityService);
                    var activeNetwork = cm.ActiveNetworkInfo;
                    if (activeNetwork != null && activeNetwork.IsConnectedOrConnecting) {
                        switch (activeNetwork.Type) {
                        case Android.Net.ConnectivityType.Wifi:
                            return "wifi";
                        case Android.Net.ConnectivityType.Ethernet:
                            return "ethernet";
                        default:
                            return "cellular";
                        }
                    } else {
                        return "none";
                    }
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to determine network status.");
                    return null;
                }
            }

            public static bool CheckMemoryLow (Context ctx)
            {
                try {
                    var am = (Android.App.ActivityManager)ctx.GetSystemService (
                                 Android.Content.Context.ActivityService);
                    var memInfo = new Android.App.ActivityManager.MemoryInfo ();
                    am.GetMemoryInfo (memInfo);

                    return memInfo.LowMemory;
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to determine low memory state.");
                    return false;
                }
            }

            public static string GetAppVersion (Context ctx)
            {
                try {
                    var pkg = ctx.PackageManager.GetPackageInfo (ctx.PackageName, 0);
                    return pkg.VersionName;
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to get the application version.");
                    return null;
                }
            }

            public static string GetAppName (Context ctx)
            {
                try {
                    var app = ctx.PackageManager.GetApplicationInfo (ctx.PackageName, 0);
                    return app.Name;
                } catch (Java.Lang.Throwable ex) {
                    Log.Warn (Tag, ex, "Failed to get the application name.");
                    return null;
                }
            }
        }
    }
}
