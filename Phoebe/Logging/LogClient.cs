using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.Messages;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe.Logging
{
    public class LogClient : ILoggerClient
    {
        #region ILoggerClient implementation

        // TODO RX Set or find a way to set the user everytime it is needed.
        // reading from the state maybe.
        public void SetUser (string id, string email = null, string name = null)
        {
            #if (!DEBUG)
            if (id != null) {
                RaygunClient.Current.UserInfo = new RaygunIdentifierMessage (id) {
                    IsAnonymous = false,
                    Email = email,
                    FullName = name
                };
            } else {
                RaygunClient.Current.UserInfo = new RaygunIdentifierMessage ("not_logged") {
                    IsAnonymous = true
                };
            }
            #endif
        }

        public void Notify (Exception e, ErrorSeverity severity = ErrorSeverity.Error, Metadata extraMetadata = null)
        {
            #if (!DEBUG)
            var extraData = new Dictionary<string, string> ();
            foreach (var item in extraMetadata) {
                if (item.Value != null) {
                    var data = item.Value.ToObject<Dictionary<string, string>>();
                    foreach (var i in data) {
                        extraData.Add ( item.Key + ":" + i.Key, i.Value);
                    }
                }
            }

            var tags = new List<string> { Enum.GetName (typeof (ErrorSeverity), severity) };
            RaygunClient.Current.SendInBackground (e, tags, extraData);
            #endif
        }

        #endregion

    }
}

