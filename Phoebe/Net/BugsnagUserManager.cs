using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Bugsnag;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.Net
{
    public class BugsnagUserManager
    {
        #pragma warning disable 0414
        private readonly object subscriptionAuthChanged;
        private readonly object subscriptionModelChanged;
        #pragma warning restore 0414
        private UserModel currentUser;

        public BugsnagUserManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChangedMessage);
            subscriptionAuthChanged = bus.Subscribe<ModelChangedMessage> (OnModelChangedMessage);

            currentUser = ServiceContainer.Resolve<AuthManager> ().User;
            OnUserChanged ();
        }

        private void OnAuthChangedMessage (AuthChangedMessage msg)
        {
            currentUser = ServiceContainer.Resolve<AuthManager> ().User;
            OnUserChanged ();
        }

        private void OnModelChangedMessage (ModelChangedMessage msg)
        {
            if (currentUser == null || msg.Model != currentUser)
                return;

            if (msg.PropertyName == UserModel.PropertyRemoteId
                || msg.PropertyName == UserModel.PropertyEmail
                || msg.PropertyName == UserModel.PropertyName) {
                OnUserChanged ();
            }
        }

        private void OnUserChanged ()
        {
            var bugsnag = ServiceContainer.Resolve<BugsnagClient> ();
            if (currentUser == null) {
                bugsnag.SetUser (null, null, null);
            } else {
                string id = currentUser.RemoteId.HasValue ? currentUser.RemoteId.ToString () : null;
                bugsnag.SetUser (id, currentUser.Email, currentUser.Name);
            }
        }
    }
}
