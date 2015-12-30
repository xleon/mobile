#if __TESTS__
#else
using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.Messages;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe.Logging
{
    public class LogClient : ILoggerClient
    {
        private readonly RaygunClient client = new RaygunClient (Build.RaygunApiKey);

        public LogClient (Action platformInitAction)
        {
            if (platformInitAction != null) {
                platformInitAction();
            }
        }

        #region ILoggerClient implementation

        public void SetUser (string id, string email = null, string name = null)
        {
            if (id != null) {
                client.UserInfo = new RaygunIdentifierMessage (id) {
                    IsAnonymous = false,
                    Email = email,
                    FullName = name
                };
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

            var tags = new List<string> { Enum.GetName (typeof (ErrorSeverity), severity) };
            client.SendInBackground (e, tags, extraData);
        }

        public string DeviceId { get; set; }

        public List<string> ProjectNamespaces { get; set; }

        #endregion

    }
}
#endif

