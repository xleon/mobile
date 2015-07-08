using System;
using UIKit;
using Toggl.Phoebe;
using Xamarin.Auth;

namespace Toggl.Ross.Net
{
    public class OAuthManager
    {
        public Action<string, bool> Authenticated;

        private OAuth2Authenticator authenticator;
        private OAuth2Authenticator Authenticator
        {
            get {
                authenticator = new OAuth2Authenticator (
                    clientId: Build.GoogleOAuthClientId,
                    clientSecret: Build.GoogleOAuthSecret,
                    scope: "email",
                    authorizeUrl: new Uri (Build.GoogleOAuthAuthorizeUrl),
                    redirectUrl: new Uri (Build.GoogleOAuthRedirectUrl),
                    accessTokenUrl: new Uri (Build.GoogleOAuthAccessTokenUrl),
                    getUsernameAsync: null
                );

                authenticator.Completed += (sender, e) => ParseArgs (e);
                return authenticator;
            }
        }

        public UIViewController UI
        {
            get {
                return Authenticator.GetUI ();
            }
        }

        private void ParseArgs (AuthenticatorCompletedEventArgs args)
        {
            if (Authenticated == null) {
                return;
            }

            if (!args.IsAuthenticated) {
                Authenticated (null, false);
                return;
            }

            string token = null;
            args.Account.Properties.TryGetValue ("access_token", out token);

            if (token != null) {
                Authenticated (token, false);
            } else {
                Authenticated (null, true);
            }
        }

        public OAuthManager ()
        {
        }
    }
}

