using System.ComponentModel;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class LoggerUserManager
    {
        public LoggerUserManager ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            authManager.PropertyChanged += OnAuthPropertyChanged;
            UpdateUser ();
        }

        private void OnAuthPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == AuthManager.PropertyUser) {
                UpdateUser ();
            }
        }

        private void UpdateUser ()
        {
            var loggerClient = ServiceContainer.Resolve<ILoggerClient> ();
            var user = ServiceContainer.Resolve<AuthManager> ().User;

            if (user == null) {
                loggerClient.SetUser (null, null, null);
            } else {
                string id = user.RemoteId.HasValue ? user.RemoteId.ToString () : null;
                loggerClient.SetUser (id, user.Email, user.Name);
            }
        }
    }
}
