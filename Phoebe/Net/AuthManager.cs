using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class AuthManager : ObservableObject
    {
        private static readonly string Tag = "AuthManager";

        private static string GetPropertyName<T> (Expression<Func<AuthManager, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public AuthManager ()
        {
            var credStore = ServiceContainer.Resolve<ISettingsStore> ();
            try {
                UserId = credStore.UserId;
                Token = credStore.ApiToken;
                IsAuthenticated = !String.IsNullOrEmpty (Token);
            } catch (ArgumentException) {
                // When data is corrupt and cannot find user
                credStore.UserId = null;
                credStore.ApiToken = null;
            }
        }

        private async Task<bool> Authenticate (Func<Task<UserModel>> getUser)
        {
            if (IsAuthenticated)
                throw new InvalidOperationException ("Cannot authenticate when old credentials still present.");
            if (IsAuthenticating)
                throw new InvalidOperationException ("Another authentication is still in progress.");

            IsAuthenticating = true;

            try {
                UserModel user;
                try {
                    user = await getUser ();
                    if (user == null) {
                        ServiceContainer.Resolve<MessageBus> ().Send (
                            new AuthFailedMessage (this, AuthFailedMessage.Reason.InvalidCredentials));
                        return false;
                    }
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<Logger> ();
                    if (ex is System.Net.Http.HttpRequestException) {
                        log.Info (Tag, ex, "Failed authenticate user.");
                    } else {
                        log.Warning (Tag, ex, "Failed to authenticate user.");
                    }

                    ServiceContainer.Resolve<MessageBus> ().Send (
                        new AuthFailedMessage (this, AuthFailedMessage.Reason.InvalidCredentials, ex));
                    return false;
                }

                var credStore = ServiceContainer.Resolve<ISettingsStore> ();
                credStore.UserId = user.Id;
                credStore.ApiToken = user.ApiToken;

                user.IsPersisted = true;
                UserId = user.Id;
                Token = user.ApiToken;
                IsAuthenticated = true;

                ServiceContainer.Resolve<MessageBus> ().Send (
                    new AuthChangedMessage (this));
            } finally {
                IsAuthenticating = false;
            }

            return true;
        }

        public Task<bool> Authenticate (string username, string password)
        {
            var client = ServiceContainer.Resolve<ITogglClient> ();
            return Authenticate (() => client.GetUser (username, password));
        }

        public Task<bool> AuthenticateWithGoogle (string accessToken)
        {
            var client = ServiceContainer.Resolve<ITogglClient> ();
            return Authenticate (() => client.GetUser (accessToken));
        }

        public void Forget ()
        {
            if (IsAuthenticated)
                throw new InvalidOperationException ("Cannot forget credentials which don't exist.");

            var credStore = ServiceContainer.Resolve<ISettingsStore> ();
            credStore.UserId = null;
            credStore.ApiToken = null;

            IsAuthenticated = false;
            Token = null;
            UserId = null;

            ServiceContainer.Resolve<MessageBus> ().Send (
                new AuthChangedMessage (this));
        }

        private bool authenticating;
        public static readonly string PropertyIsAuthenticating = GetPropertyName ((m) => m.IsAuthenticating);

        public bool IsAuthenticating {
            get { return authenticating; }
            private set {
                if (authenticating == value)
                    return;

                ChangePropertyAndNotify (PropertyIsAuthenticating, delegate {
                    authenticating = value;
                });
            }
        }

        private bool authenticated;
        public static readonly string PropertyIsAuthenticated = GetPropertyName ((m) => m.IsAuthenticated);

        public bool IsAuthenticated {
            get { return authenticated; }
            private set {
                if (authenticated == value)
                    return;

                ChangePropertyAndNotify (PropertyIsAuthenticated, delegate {
                    authenticated = value;
                });
            }
        }

        private Guid? userId;
        public static readonly string PropertyUserId = GetPropertyName ((m) => m.UserId);

        public Guid? UserId {
            get { return userId; }
            private set {
                if (userId == value)
                    return;

                UserModel model = null;
                if (value.HasValue) {
                    model = Model.ById<UserModel> (value.Value);
                    if (model == null)
                        throw new ArgumentException ("Unable to resolve UserId to model.");
                }

                ChangePropertyAndNotify (PropertyUserId, delegate {
                    userId = value;
                });

                User = model;
            }
        }

        private UserModel user;
        public static readonly string PropertyUser = GetPropertyName ((m) => m.User);

        public UserModel User {
            get { return user; }
            private set {
                if (user == value)
                    return;
                ChangePropertyAndNotify (PropertyUser, delegate {
                    user = value;
                });
            }
        }

        private string token;
        public static readonly string PropertyToken = GetPropertyName ((m) => m.Token);

        public string Token {
            get { return token; }
            private set {
                if (token == value)
                    return;

                ChangePropertyAndNotify (PropertyToken, delegate {
                    token = value;
                });
            }
        }
    }
}
