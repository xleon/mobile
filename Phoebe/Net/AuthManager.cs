using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;
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

        private readonly Subscription<DataChangeMessage> subscriptionDataChange;

        public AuthManager ()
        {
            var credStore = ServiceContainer.Resolve<ISettingsStore> ();
            try {
                if (credStore.UserId.HasValue) {
                    User = new UserData () {
                        Id = credStore.UserId.Value,
                    };
                    // Load full user data:
                    ReloadUser ();
                }
                Token = credStore.ApiToken;
                IsAuthenticated = !String.IsNullOrEmpty (Token);
            } catch (ArgumentException) {
                // When data is corrupt and cannot find user
                credStore.UserId = null;
                credStore.ApiToken = null;
            }

            // Listen for global data changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
        }

        private async void ReloadUser ()
        {
            if (User == null) {
                return;
            }
            var store = ServiceContainer.Resolve<IDataStore> ();
            var rows = await store.Table<UserData> ().QueryAsync (r => r.Id == User.Id);
            User = rows.FirstOrDefault ();
        }

        private async Task<bool> Authenticate (Func<Task<UserJson>> getUser)
        {
            if (IsAuthenticated) {
                throw new InvalidOperationException ("Cannot authenticate when old credentials still present.");
            }
            if (IsAuthenticating) {
                throw new InvalidOperationException ("Another authentication is still in progress.");
            }

            IsAuthenticating = true;

            try {
                UserJson userJson;
                try {
                    userJson = await getUser ();
                    if (userJson == null) {
                        ServiceContainer.Resolve<MessageBus> ().Send (
                            new AuthFailedMessage (this, AuthFailedMessage.Reason.InvalidCredentials));
                        return false;
                    }
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<Logger> ();
                    if (ex.IsNetworkFailure () || ex is TaskCanceledException) {
                        log.Info (Tag, ex, "Failed authenticate user.");
                    } else {
                        log.Warning (Tag, ex, "Failed to authenticate user.");
                    }

                    ServiceContainer.Resolve<MessageBus> ().Send (
                        new AuthFailedMessage (this, AuthFailedMessage.Reason.NetworkError, ex));
                    return false;
                }

                // Import the user into our database:
                UserData userData;
                try {
                    var dataStore = ServiceContainer.Resolve<IDataStore> ();
                    userData = await dataStore.ExecuteInTransactionAsync (ctx => userJson.Import (ctx));
                } catch (Exception ex) {
                    var log = ServiceContainer.Resolve<Logger> ();
                    log.Error (Tag, ex, "Failed to import authenticated user.");

                    ServiceContainer.Resolve<MessageBus> ().Send (
                        new AuthFailedMessage (this, AuthFailedMessage.Reason.SystemError, ex));
                    return false;
                }

                var credStore = ServiceContainer.Resolve<ISettingsStore> ();
                credStore.UserId = userData.Id;
                credStore.ApiToken = userJson.ApiToken;

                User = userData;
                Token = userJson.ApiToken;
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
            var log = ServiceContainer.Resolve<Logger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Authenticating with email ({0}).", username);
            return Authenticate (() => client.GetUser (username, password));
        }

        public Task<bool> AuthenticateWithGoogle (string accessToken)
        {
            var log = ServiceContainer.Resolve<Logger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Authenticating with Google access token.");
            return Authenticate (() => client.GetUser (accessToken));
        }

        public Task<bool> Signup (string email, string password)
        {
            var log = ServiceContainer.Resolve<Logger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Signing up with email ({0}).", email);
            return Authenticate (() => client.Create (new UserJson () {
                Email = email,
                Password = password,
                Timezone = Time.TimeZoneId,
            }));
        }

        public Task<bool> SignupWithGoogle (string accessToken)
        {
            var log = ServiceContainer.Resolve<Logger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Signing up with email Google access token.");
            return Authenticate (() => client.Create (new UserJson () {
                GoogleAccessToken = accessToken,
                Timezone = Time.TimeZoneId,
            }));
        }

        public void Forget ()
        {
            if (!IsAuthenticated) {
                throw new InvalidOperationException ("Cannot forget credentials which don't exist.");
            }

            var log = ServiceContainer.Resolve<Logger> ();
            log.Info (Tag, "Forgetting current user.");

            var credStore = ServiceContainer.Resolve<ISettingsStore> ();
            credStore.UserId = null;
            credStore.ApiToken = null;

            IsAuthenticated = false;
            Token = null;
            User = null;

            ServiceContainer.Resolve<MessageBus> ().Send (
                new AuthChangedMessage (this));
        }

        private void OnDataChange (DataChangeMessage msg)
        {
            if (User.Matches (msg.Data)) {
                User = new UserData ((UserData)msg.Data);
            }
        }

        private bool authenticating;
        public static readonly string PropertyIsAuthenticating = GetPropertyName ((m) => m.IsAuthenticating);

        public bool IsAuthenticating
        {
            get { return authenticating; }
            private set {
                if (authenticating == value) {
                    return;
                }

                ChangePropertyAndNotify (PropertyIsAuthenticating, delegate {
                    authenticating = value;
                });
            }
        }

        private bool authenticated;
        public static readonly string PropertyIsAuthenticated = GetPropertyName ((m) => m.IsAuthenticated);

        public bool IsAuthenticated
        {
            get { return authenticated; }
            private set {
                if (authenticated == value) {
                    return;
                }

                ChangePropertyAndNotify (PropertyIsAuthenticated, delegate {
                    authenticated = value;
                });
            }
        }

        private UserData userData;
        public static readonly string PropertyUser = GetPropertyName ((m) => m.User);

        public UserData User
        {
            get { return userData; }
            private set {
                if (userData == value) {
                    return;
                }

                ChangePropertyAndNotify (PropertyUser, delegate {
                    userData = value;
                });
            }
        }

        public Guid? GetUserId ()
        {
            if (User == null) {
                return null;
            }
            return User.Id;
        }

        private string token;
        public static readonly string PropertyToken = GetPropertyName ((m) => m.Token);

        public string Token
        {
            get { return token; }
            private set {
                if (token == value) {
                    return;
                }

                ChangePropertyAndNotify (PropertyToken, delegate {
                    token = value;
                });
            }
        }
    }
}
