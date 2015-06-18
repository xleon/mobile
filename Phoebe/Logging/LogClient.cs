using System;
using System.Collections.Generic;
using Xamarin;
using Android.Content;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe.Logging
{
    public class LogClient : ILoggerClient
    {
        public LogClient (string xamApiKey, string bugsnagApiKey, bool enableMetrics = true, Context context = null)
        {
            #if __IOS__
                #if DEBUG
                Insights.Initialize (Insights.DebugModeKey);
                #else
                Insights.Initialize (xamApiKey);
                #endif
            #endif

            #if __ANDROID__
                if (context == null) {
                    throw new ArgumentNullException();
                }
                #if DEBUG
                Insights.Initialize (Insights.DebugModeKey, context);
                #else
                Insights.Initialize (xamApiKeyt, contex);
                #endif
            #endif
        }

        #region ILoggerClient implementation

        public void SetUser (string id, string email = null, string name = null)
        {
            if (id != null) {
                var traits = new Dictionary<string, string> {
                    { Insights.Traits.Email, email },
                    { Insights.Traits.Name, name }
                };
                Insights.Identify (id, traits);
            }
        }

        public void Notify (Exception e, ErrorSeverity severity = ErrorSeverity.Error, Metadata extraMetadata = null)
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
            
        #endregion

    }
}

