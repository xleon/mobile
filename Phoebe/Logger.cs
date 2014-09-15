using System;
using Bugsnag;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public class Logger
    {
        public enum Level
        {
            Debug,
            Info,
            Warning,
            Error,
        }

        private readonly Level threshold;
        private readonly Level consoleThreshold;

        public Logger ()
        {
            #if DEBUG
            threshold = Level.Debug;
            consoleThreshold = Level.Debug;
            #else
            threshold = Level.Info;
            consoleThreshold = Level.Warning;
            #endif
        }

        public Logger (Level threshold)
        {
            this.threshold = threshold;
        }

        private void Process (Level level, string tag, string message, Exception exc = null)
        {
            LogToConsole (level, tag, message, exc);
            LogToFile (level, tag, message, exc);
            LogToBugsnag (level, tag, message, exc);
        }

        private void LogToConsole (Level level, string tag, string message, Exception exc)
        {
            if (level < consoleThreshold)
                return;

            WriteConsole (level, tag, message, exc);
        }

        private void LogToFile (Level level, string tag, string message, Exception exc)
        {
            var logStore = ServiceContainer.Resolve<LogStore> ();
            if (logStore != null) {
                logStore.Record (level, tag, message, exc);
            }
        }

        private void LogToBugsnag (Level level, string tag, string message, Exception exc)
        {
            // Send warnings and errors to Bugsnag:
            if (level >= Level.Warning) {
                Bugsnag.Data.ErrorSeverity severity;

                switch (level) {
                case Level.Warning:
                    severity = Bugsnag.Data.ErrorSeverity.Warning;
                    break;
                case Level.Error:
                    severity = Bugsnag.Data.ErrorSeverity.Error;
                    break;
                default:
                    severity = Bugsnag.Data.ErrorSeverity.Info;
                    break;
                }

                var md = new Bugsnag.Data.Metadata ();
                md.AddToTab ("Logger", "Tag", tag);
                md.AddToTab ("Logger", "Message", message);

                var bugsnagClient = ServiceContainer.Resolve<IBugsnagClient> ();
                if (bugsnagClient != null) {
                    bugsnagClient.Notify (exc, severity, md);
                }
            }
        }

        protected virtual void WriteConsole (Level level, string tag, string message, Exception exc)
        {
            Console.WriteLine ("[{1}] {0}: {2}", level, tag, message);
            if (exc != null) {
                Console.WriteLine (exc.ToString ());
            }
        }

        public void Debug (string tag, string message)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, message);
        }

        public void Debug (string tag, string message, object arg0)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, arg0));
        }

        public void Debug (string tag, string message, object arg0, object arg1)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, arg0, arg1));
        }

        public void Debug (string tag, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, arg0, arg1, arg2));
        }

        public void Debug (string tag, string message, params object[] args)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, args));
        }

        public void Debug (string tag, Exception exc, string message)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, message, exc);
        }

        public void Debug (string tag, Exception exc, string message, object arg0)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, arg0), exc);
        }

        public void Debug (string tag, Exception exc, string message, object arg0, object arg1)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, arg0, arg1), exc);
        }

        public void Debug (string tag, Exception exc, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, arg0, arg1, arg2), exc);
        }

        public void Debug (string tag, Exception exc, string message, params object[] args)
        {
            if (threshold > Level.Debug)
                return;
            Process (Level.Debug, tag, String.Format (message, args), exc);
        }

        public void Info (string tag, string message)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, message);
        }

        public void Info (string tag, string message, object arg0)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, arg0));
        }

        public void Info (string tag, string message, object arg0, object arg1)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, arg0, arg1));
        }

        public void Info (string tag, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, arg0, arg1, arg2));
        }

        public void Info (string tag, string message, params object[] args)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, args));
        }

        public void Info (string tag, Exception exc, string message)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, message, exc);
        }

        public void Info (string tag, Exception exc, string message, object arg0)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, arg0), exc);
        }

        public void Info (string tag, Exception exc, string message, object arg0, object arg1)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, arg0, arg1), exc);
        }

        public void Info (string tag, Exception exc, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, arg0, arg1, arg2), exc);
        }

        public void Info (string tag, Exception exc, string message, params object[] args)
        {
            if (threshold > Level.Info)
                return;
            Process (Level.Info, tag, String.Format (message, args), exc);
        }

        public void Warning (string tag, string message)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, message);
        }

        public void Warning (string tag, string message, object arg0)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, arg0));
        }

        public void Warning (string tag, string message, object arg0, object arg1)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, arg0, arg1));
        }

        public void Warning (string tag, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, arg0, arg1, arg2));
        }

        public void Warning (string tag, string message, params object[] args)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, args));
        }

        public void Warning (string tag, Exception exc, string message)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, message, exc);
        }

        public void Warning (string tag, Exception exc, string message, object arg0)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, arg0), exc);
        }

        public void Warning (string tag, Exception exc, string message, object arg0, object arg1)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, arg0, arg1), exc);
        }

        public void Warning (string tag, Exception exc, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, arg0, arg1, arg2), exc);
        }

        public void Warning (string tag, Exception exc, string message, params object[] args)
        {
            if (threshold > Level.Warning)
                return;
            Process (Level.Warning, tag, String.Format (message, args), exc);
        }

        public void Error (string tag, string message)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, message);
        }

        public void Error (string tag, string message, object arg0)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, arg0));
        }

        public void Error (string tag, string message, object arg0, object arg1)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, arg0, arg1));
        }

        public void Error (string tag, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, arg0, arg1, arg2));
        }

        public void Error (string tag, string message, params object[] args)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, args));
        }

        public void Error (string tag, Exception exc, string message)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, message, exc);
        }

        public void Error (string tag, Exception exc, string message, object arg0)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, arg0), exc);
        }

        public void Error (string tag, Exception exc, string message, object arg0, object arg1)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, arg0, arg1), exc);
        }

        public void Error (string tag, Exception exc, string message, object arg0, object arg1, object arg2)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, arg0, arg1, arg2), exc);
        }

        public void Error (string tag, Exception exc, string message, params object[] args)
        {
            if (threshold > Level.Error)
                return;
            Process (Level.Error, tag, String.Format (message, args), exc);
        }
    }
}
