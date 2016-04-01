using System;
using System.Threading.Tasks;
using Cirrious.FluentLayouts.Touch;
using GalaSoft.MvvmLight.Helpers;
using Google.SignIn;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.ViewModels;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class WelcomeViewController : UIViewController, ISignInDelegate, ISignInUIDelegate
    {
        private const string Tag = "WelcomeViewController";

        private UINavigationController navController;
        private UIImageView logoImageView;
        private UILabel sloganLabel;
        private UIButton createButton;
        private UIButton passwordButton;
        private UIButton googleButton;
        private LoginVM viewModel {get; set;}

        public WelcomeViewController()
        {
            viewModel = new LoginVM ();
        }

        public override void LoadView ()
        {
            View = new UIImageView () {
                UserInteractionEnabled = true,
            } .Apply (Style.Welcome.Background);
            View.Add (logoImageView = new UIImageView ().Apply (Style.Welcome.Logo));
            View.Add (sloganLabel = new UILabel () {
                Text = "WelcomeSlogan".Tr (),
            } .Apply (Style.Welcome.Slogan));
            View.Add (createButton = new UIButton ().Apply (Style.Welcome.CreateAccount));
            View.Add (passwordButton = new UIButton ().Apply (Style.Welcome.PasswordLogin));
            View.Add (googleButton = new UIButton ().Apply (Style.Welcome.GoogleLogin));

            createButton.SetTitle ("WelcomeCreate".Tr (), UIControlState.Normal);
            passwordButton.SetTitle ("WelcomePassword".Tr (), UIControlState.Normal);
            googleButton.SetTitle ("WelcomeGoogle".Tr (), UIControlState.Normal);

            createButton.TouchUpInside += OnCreateButtonTouchUpInside;
            passwordButton.TouchUpInside += OnPasswordButtonTouchUpInside;
            googleButton.TouchUpInside += OnGoogleButtonTouchUpInside;

            View.AddConstraints (
                logoImageView.AtTopOf (View, 70f),
                logoImageView.WithSameCenterX (View),

                sloganLabel.Below (logoImageView, 18f),
                sloganLabel.AtLeftOf (View, 25f),
                sloganLabel.AtRightOf (View, 25f),

                googleButton.AtBottomOf (View, 20f),
                googleButton.AtLeftOf (View),
                googleButton.AtRightOf (View),
                googleButton.Height ().EqualTo (60f),

                passwordButton.Above (googleButton, 25f),
                passwordButton.AtLeftOf (View),
                passwordButton.AtRightOf (View),
                passwordButton.Height ().EqualTo (60f),

                createButton.Above (passwordButton, 5f),
                createButton.AtLeftOf (View),
                createButton.AtRightOf (View),
                createButton.Height ().EqualTo (60f)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();
        }

        private void OnCreateButtonTouchUpInside (object sender, EventArgs e)
        {
            NavigationController.PushViewController (new SignupViewController (), true);
        }

        private void OnPasswordButtonTouchUpInside (object sender, EventArgs e)
        {
            NavigationController.PushViewController (new LoginViewController (), true);
        }

        private void OnGoogleButtonTouchUpInside (object sender, EventArgs e)
        {
            // Automatically sign in the user.
            SignIn.SharedInstance.SignInUser ();
        }

        public void DidSignIn (SignIn signIn, GoogleUser user, Foundation.NSError error)
        {
            if (error == null) {
                var token = user.Authentication.AccessToken;
                var email = user.Profile.Email;
                signIn.DisconnectUser (); // Disconnect user from Google.
                viewModel.TryLoginWithGoogle (token);
            } else if (error.Code != -5) { // Cancel error code.
                new UIAlertView ("WelcomeGoogleErrorTitle".Tr (), "WelcomeGoogleErrorMessage".Tr (), null, "WelcomeGoogleErrorOk".Tr (), null).Show ();
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Info (Tag, "Failed to authenticate (G+) the user.");
                IsAuthenticating = false;
            }
        }

        private Binding<bool, bool> isAuthencticatedBinding, isAuthenticatingBinding;
        private Binding<AuthResult, AuthResult> resultBinding;

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            SignIn.SharedInstance.Delegate = this;
            SignIn.SharedInstance.UIDelegate = this;

            isAuthenticatingBinding = this.SetBinding (() => viewModel.IsAuthenticating).WhenSourceChanges (SetViewState);
            resultBinding = this.SetBinding (() => viewModel.AuthResult).WhenSourceChanges (() => {
                switch (viewModel.AuthResult) {
                case AuthResult.None:
                case AuthResult.Authenticating:
                    IsAuthenticating = true;
                    break;

                case AuthResult.Success:
                    // TODO RX: Start the initial sync for the user
                    //ServiceContainer.Resolve<ISyncManager> ().Run ();
                    var intent = new Intent (this, typeof (MainDrawerActivity));
                    intent.AddFlags (ActivityFlags.ClearTop);
                    StartActivity (intent);
                    Finish ();
                    break;

                // Error cases
                default:
                    if (viewModel.CurrentLoginMode == LoginVM.LoginMode.Login) {
                        if (viewModel.AuthResult == AuthResult.InvalidCredentials) {
                            PasswordEditText.Text = string.Empty;
                        }
                        PasswordEditText.RequestFocus ();
                    } else {
                        EmailEditText.RequestFocus ();
                    }
                    ShowAuthError (EmailEditText.Text, ViewModel.AuthResult, ViewModel.CurrentLoginMode);
                    break;
                }
            });

            navController = NavigationController;
            if (navController != null) {
                navController.SetNavigationBarHidden (true, animated);
            }
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Welcome";
        }

        public override void ViewWillDisappear (bool animated)
        {
            base.ViewWillDisappear (animated);

            if (navController != null) {
                navController.SetNavigationBarHidden (false, animated);
                navController = null;
            }

            if (SignIn.SharedInstance.Delegate == this) {
                SignIn.SharedInstance.Delegate = null;
                SignIn.SharedInstance.UIDelegate = this;
            }
        }

        private bool IsAuthenticating
        {
            get { return !View.UserInteractionEnabled; }
            set {
                if (View.UserInteractionEnabled == !value) {
                    return;
                }

                View.UserInteractionEnabled = !value;
                UIView.Animate (0.3, 0,
                                UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseInOut,
                delegate {
                    createButton.Alpha = value ? 0 : 1;
                    googleButton.Alpha = value ? 0 : 1;
                },
                delegate {
                }
                               );
                passwordButton.SetTitle (value ? "WelcomeLoggingIn".Tr () : "WelcomePassword".Tr (), UIControlState.Normal);
            }
        }

        public void AuthWithGoogleTokenAsync (SignIn signIn, GoogleUser user, Foundation.NSError error)
        {



            try {
                if (error == null) {
                    IsAuthenticating = true;
                    var token = user.Authentication.AccessToken;

                    // No need to keep the users Google account access around anymore

                    if (authRes != AuthResult.Success) {

                        AuthErrorAlert.Show (this, email, authRes, AuthErrorAlert.Mode.Login, true);
                    } else {
                        // Start the initial sync for the user
                        ServiceContainer.Resolve<ISyncManager> ().Run ();
                    }
                } else if (error.Code != -5) { // Cancel error code.
                    new UIAlertView ("WelcomeGoogleErrorTitle".Tr (), "WelcomeGoogleErrorMessage".Tr (), null, "WelcomeGoogleErrorOk".Tr (), null).Show ();
                }
            } catch (InvalidOperationException ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Info (Tag, ex, "Failed to authenticate (G+) the user.");
            } finally {
                IsAuthenticating = false;
            }
        }
    }
}
