using System;
using System.Collections.Generic;
using Xamarin;
using Android.Content;
using Toggl.Phoebe.Logging;

namespace Toggl.Joey.Logging
{
    public class LogClient : ILoggerClient
    {
        public LogClient (Context context, string xamApiKey, string bugsnagApiKey, bool enableMetrics = true)
        {
            #if DEBUG
            Insights.Initialize (Insights.DebugModeKey, context);
            #else
            Insights.Initialize (xamApiKeyt, contex);
            #endif
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
        }

        public void ClearTab (string tabName)
        {
        }


        #endregion


    }
}

