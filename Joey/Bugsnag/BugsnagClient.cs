using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Util;
using Toggl.Phoebe.Bugsnag.Data;

namespace Toggl.Joey.Bugsnag
{
    public class BugsnagClient : Toggl.Phoebe.Bugsnag.BugsnagClient
    {
        private static readonly TimeSpan IdleTimeForSessionEnd = TimeSpan.FromSeconds (10);
        private static readonly string Tag = "Bugsense";
        private readonly Context androidContext;
        private readonly List<WeakReference> activityStack = new List<WeakReference> ();
        private readonly bool sendMetrics;
        private bool isInitialised;
        private WeakReference topActivity;
        private DateTime appStartTime;
        private DateTime sessionPauseTime;
        private DateTime sessionStartTime;
        private SystemInfo systemInfo;
        private ApplicationInfo appInfo;

        public BugsnagClient (Context context, string apiKey, bool enableMetrics = true) : base (apiKey)
        {
            sendMetrics = enableMetrics;
            androidContext = context.ApplicationContext;
            appStartTime = DateTime.UtcNow;
            GuessReleaseStage ();

            JavaExceptionHandler.Install (this);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                JavaExceptionHandler.CleanUp ();
            }

            base.Dispose (disposing);
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
                TrackUser ();
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

        private void FlushReports ()
        {
            // TODO: Send all events serialized from disk
            throw new NotImplementedException ();
        }

        protected override void SendEvent (Event e)
        {
            /* TODO:
             * - Serialize event to disk
             * - Try sending data to bugsense
             * - On success delete disk file
             */
            throw new NotImplementedException ();
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

        protected override ApplicationState GetAppState ()
        {
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
                    TotalMemory = AndroidInfo.GetMemoryAvailable (),
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
            return new Toggl.Joey.Bugsnag.Data.SystemState () {
                FreeMemory = AndroidInfo.GetFreeMemory (),
                Orientation = AndroidInfo.GetOrientation (androidContext),
                BatteryLevel = AndroidInfo.GetBatteryLevel (androidContext),
                IsCharging = AndroidInfo.CheckBatteryCharging (androidContext),
                AvailableDiskSpace = AndroidInfo.GetAvailableDiskSpace (),
                LocationStatus = AndroidInfo.GetGpsStatus (androidContext),
                NetworkStatus = AndroidInfo.GetNetworkStatus (androidContext),
            };
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
