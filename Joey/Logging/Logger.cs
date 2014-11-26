using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Logging;

namespace Toggl.Joey.Logging
{
    public class Logger : BaseLogger
    {
        public Logger () : base ()
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

        protected override void AddExtraMetadata (Bugsnag.Data.Metadata md)
        {
            // TODO: Add user settings
        }
    }
}
