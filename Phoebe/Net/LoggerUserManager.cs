using System.ComponentModel;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class LoggerUserManager
    {
        public LoggerUserManager ()
        {
            UpdateUser ();
        }

        private void OnAuthPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            //if (args.PropertyName == AuthManager.PropertyUser) {
            UpdateUser ();
            //}
        }

        private void UpdateUser ()
        {
            var loggerClient = ServiceContainer.Resolve<ILoggerClient> ();
            var user = new UserData ();//ServiceContainer.Resolve<AuthManager> ().User;

            if (user == null) {
                loggerClient.SetUser (null, null, null);
            } else {
                string id = user.RemoteId.HasValue ? user.RemoteId.ToString () : null;
                loggerClient.SetUser (id, user.Email, user.Name);
            }
        }
    }
}
