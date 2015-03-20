using System;
using System.Collections.Generic;
using Android.Content;
using Bugsnag;
using Xamarin;

namespace Toggl.Joey.Logging
{
    public class LogClient : IBugsnagClient
    {
        private BugsnagClient bugsnagClient;

        public LogClient (Context context, string xamApiKey, string bugsnagApiKey, bool enableMetrics = true)
        {
            #if DEBUG
            Insights.Initialize (Insights.DebugModeKey, context);
            #else
            Insights.Initialize (xamApiKey, context);
            #endif
            bugsnagClient = new BugsnagClient (context, bugsnagApiKey, enableMetrics);
        }

        #region IBugsnagClient implementation

        void IBugsnagClient.SetUser (string id, string email, string name)
        {
            if (id != null) {
                var traits = new Dictionary<string, string> {
                    { Insights.Traits.Email, email },
                    { Insights.Traits.Name, name }
                };
                Insights.Identify (id, traits);
            }

            bugsnagClient.SetUser (id, email, name);
        }

        void IBugsnagClient.TrackUser ()
        {
            bugsnagClient.TrackUser ();
        }

        void IBugsnagClient.Notify (Exception e, Bugsnag.Data.ErrorSeverity severity, Bugsnag.Data.Metadata extraMetadata)
        {
            var reportSeverity = ReportSeverity.Error;
            if (severity == Bugsnag.Data.ErrorSeverity.Warning) {
                reportSeverity = ReportSeverity.Warning;
            }

            var extraData = new Dictionary<string, string> ();
            foreach (var item in extraMetadata) {
                if (item.Value != null) {
                    var data = item.Value.ToObject<Dictionary<string, string>>();
                    foreach (var i in data) {
                        extraData.Add ( item.Key + ":" + i.Key, i.Value);
                    }
                }
            }

            if (severity == Bugsnag.Data.ErrorSeverity.Info) {
                Insights.Track ("Info", extraData);
            } else {
                Insights.Report (e, extraData, reportSeverity);
            }

            bugsnagClient.Notify (e, severity, extraMetadata);
        }

        string IBugsnagClient.UserId
        {
            get {
                return bugsnagClient.UserId;
            } set {
                bugsnagClient.UserId = value;
            }
        }

        string IBugsnagClient.UserEmail
        {
            get {
                return bugsnagClient.UserEmail;
            } set {
                bugsnagClient.UserEmail = value;
            }
        }

        string IBugsnagClient.UserName
        {
            get {
                return bugsnagClient.UserName;
            } set {
                bugsnagClient.UserName = value;
            }
        }

        public string DeviceId { get; set; }

        public bool AutoNotify { get; set; }

        public string Context { get; set; }

        public string ReleaseStage { get; set; }

        public List<string> NotifyReleaseStages { get; set; }

        public List<string> Filters { get; set; }

        public List<Type> IgnoredExceptions { get; set; }

        public List<string> ProjectNamespaces { get; set; }

        void IBugsnagClient.AddToTab (string tabName, string key, object value)
        {
            bugsnagClient.AddToTab (tabName, key, value);
        }

        void IBugsnagClient.ClearTab (string tabName)
        {
            bugsnagClient.ClearTab (tabName);
        }


        #endregion

        #region IDisposable implementation

        void IDisposable.Dispose ()
        {
            bugsnagClient.Dispose ();
        }

        #endregion

        public void OnActivityCreated (Context ctx)
        {
            bugsnagClient.OnActivityCreated (ctx);
        }

        public void OnActivityResumed (Context ctx)
        {
            bugsnagClient.OnActivityResumed (ctx);
        }

        public void OnActivityPaused (Context ctx)
        {
            bugsnagClient.OnActivityPaused (ctx);
        }

        public void OnActivityDestroyed (Context ctx)
        {
            bugsnagClient.OnActivityDestroyed (ctx);
        }
    }
}

