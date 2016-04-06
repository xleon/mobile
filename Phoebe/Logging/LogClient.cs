using System;
using System.Collections.Generic;
using Xamarin;

namespace Toggl.Phoebe.Logging
{
    public class LogClient : ILoggerClient
    {
        #region ILoggerClient implementation

        // TODO RX Set or find a way to set the user everytime it is needed.
        // reading from the state maybe.
        public void SetUser (string id, string email = null, string name = null)
        {
            if (id != null) {
                var traits = new Dictionary<string, string>
                {
                    { Insights.Traits.Email, email },
                    { Insights.Traits.Name, name },
                };
                Insights.Identify(id, traits);
            }
        }

        public void Notify (Exception e, ErrorSeverity severity = ErrorSeverity.Error, Metadata extraMetadata = null)
        {
            var extraData = new Dictionary<string, string> ();
            foreach (var item in extraMetadata) {
                if (item.Value != null) {
                    var data = item.Value.ToObject<Dictionary<string, string>>();
                    foreach (var i in data) {
                        extraData.Add ( item.Key + ":" + i.Key, i.Value);
                    }
                }
            }

            if (severity == ErrorSeverity.Info)
            {
                Insights.Track("Info", extraData);
            }
            else
            {
                var reportSeverity = severity == ErrorSeverity.Warning
                    ? Insights.Severity.Warning : Insights.Severity.Error;
                
                Insights.Report(e, extraData, reportSeverity);
            }
        }

        #endregion

    }
}

