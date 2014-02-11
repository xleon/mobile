using System;
using Toggl.Phoebe;

namespace Toggl.Joey
{
    public class AndroidLogger : Logger
    {
        public AndroidLogger () : base ()
        {
        }

        public AndroidLogger (Level threshold) : base (threshold)
        {
        }

        protected override void Log (Level level, string tag, string message, Exception exc)
        {
            switch (level) {
            case Level.Debug:
                if (exc == null) {
                    Android.Util.Log.Debug (tag, message);
                } else {
                    Android.Util.Log.Debug (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            case Level.Info:
                if (exc == null) {
                    Android.Util.Log.Info (tag, message);
                } else {
                    Android.Util.Log.Info (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            case Level.Warning:
                if (exc == null) {
                    Android.Util.Log.Warn (tag, message);
                } else {
                    Android.Util.Log.Warn (tag, Java.Lang.Throwable.FromException (exc), message);
                }
                break;
            case Level.Error:
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
    }
}
