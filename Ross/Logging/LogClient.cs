using System;
using System.Collections.Generic;
using Bugsnag;
using Xamarin;
using Toggl.Phoebe.Logging;

namespace Toggl.Ross.Logging
{
    public class LogClient : ILoggerClient
    {
        private BugsnagClient bugsnagClient;

        public LogClient (string xamApiKey, string bugsnagApiKey, bool enableMetrics = true)
        {
            #if DEBUG
            Insights.Initialize (Insights.DebugModeKey);
            #else
            Insights.Initialize (xamApiKey);
            #endif
            bugsnagClient = new BugsnagClient (bugsnagApiKey, enableMetrics);
        }

        #region ILoggerClient implementation

        public void SetUser (string id, string email, string name)
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

        public void TrackUser ()
        {
            bugsnagClient.TrackUser ();
        }

        public void Notify (Exception e, ErrorSeverity severity, Metadata extraMetadata)
        {
            var reportSeverity = Insights.Severity.Error;
            if (severity == ErrorSeverity.Warning) {
                reportSeverity = Insights.Severity.Warning;
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

            if (severity == ErrorSeverity.Info) {
                Insights.Track ("Info", extraData);
            } else {
                Insights.Report (e, extraData, reportSeverity);
            }

            // ISSUE INTREFACE COLLIDE ISSUES, NEEDS CONVERTER IF GONNA CONTINUE TO USE BUSGNAG
            // bugsnagClient.Notify (e, (Bugsnag.Data.ErrorSeverity)severity, (Bugsnag.Data.Metadata)extraMetadata);
        }

        public string UserId
        {
            get {
                return bugsnagClient.UserId;
            } set {
                bugsnagClient.UserId = value;
            }
        }

        public string UserEmail
        {
            get {
                return bugsnagClient.UserEmail;
            } set {
                bugsnagClient.UserEmail = value;
            }
        }

        public string UserName
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

        public void AddToTab (string tabName, string key, object value)
        {
            bugsnagClient.AddToTab (tabName, key, value);
        }

        public void ClearTab (string tabName)
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

    }
}

