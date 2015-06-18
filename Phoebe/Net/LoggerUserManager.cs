using System.ComponentModel;
using Bugsnag;
using XPlatUtils;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe.Net
{
    public class BugsnagUserManager
    {
        public BugsnagUserManager ()
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
                loggerClient.SetUser (id, null, user.Name);
            }
        }
    }
}
