using System;
using Toggl.Joey.Data;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Joey.Logging
{
    public class Logger : BaseLogger
    {
        public Logger ()
        {
        }

        public Logger (LogLevel threshold) : base (threshold)
        {
        }

        protected override void WriteConsole (LogLevel level, string tag, string message, Exception exc)
        {
            switch (level) {
            case LogLevel.Debug:
                if (exc == null) {
                    Android.Util.Log.Debug (tag, message);
                } else {
                    Android.Util.Log.Debug (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            case LogLevel.Info:
                if (exc == null) {
                    Android.Util.Log.Info (tag, message);
                } else {
                    Android.Util.Log.Info (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            case LogLevel.Warning:
                if (exc == null) {
                    Android.Util.Log.Warn (tag, message);
                } else {
                    Android.Util.Log.Warn (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            case LogLevel.Error:
                if (exc == null) {
                    Android.Util.Log.Error (tag, message);
                } else {
                    Android.Util.Log.Error (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            default:
                Android.Util.Log.Error ("Logger", String.Format ("Invalid logger level: {0}", level));
                if (exc == null) {
                    Android.Util.Log.Info (tag, message);
                } else {
                    Android.Util.Log.Info (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            }
        }

        protected override void AddExtraMetadata (Metadata md)
        {
            var settings = ServiceContainer.Resolve<SettingsStore> ();
            md.AddToTab ("State", "Experiment", settings.ExperimentId);
            md.AddToTab ("State", "Push registered", String.IsNullOrWhiteSpace (settings.GcmRegistrationId) ? "No" : "Yes");
            md.AddToTab ("State", "Got welcome message", settings.GotWelcomeMessage ? "Yes" : "No");
            md.AddToTab ("State", "Read recent continue notice", settings.ReadContinueDialog ? "Yes" : "No");
            md.AddToTab ("State", "Read duration only notice", settings.ReadDurOnlyNotice ? "Yes" : "No");

            md.AddToTab ("Settings", "Show projects for new", settings.ChooseProjectForNew ? "Yes" : "No");
            md.AddToTab ("Settings", "Idle notifications", settings.IdleNotification ? "Yes" : "No");
            md.AddToTab ("Settings", "Add default tag", settings.UseDefaultTag ? "Yes" : "No");
        }
    }
}
