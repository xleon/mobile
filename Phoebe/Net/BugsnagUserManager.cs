using System;
using System.ComponentModel;
using Toggl.Phoebe.Bugsnag;
using XPlatUtils;

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
            var bugsnag = ServiceContainer.Resolve<BugsnagClient> ();
            var user = ServiceContainer.Resolve<AuthManager> ().User;

            if (user == null) {
                bugsnag.SetUser (null, null, null);
            } else {
                string id = user.RemoteId.HasValue ? user.RemoteId.ToString () : null;
                bugsnag.SetUser (id, null, user.Name);
            }
        }
    }
}
