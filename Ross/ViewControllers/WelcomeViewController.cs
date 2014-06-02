using System;
using Cirrious.FluentLayouts.Touch;
using GoogleAnalytics.iOS;
using MonoTouch.UIKit;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class WelcomeViewController : UIViewController
    {
        private UINavigationController navController;
        private UIImageView logoImageView;
        private UILabel sloganLabel;
        private UIButton createButton;
        private UIButton passwordButton;
        private UIButton googleButton;

        public override void LoadView ()
        {
            View = new UIImageView () {
                UserInteractionEnabled = true,
            }.Apply (Style.Welcome.Background);
            View.Add (logoImageView = new UIImageView ().Apply (Style.Welcome.Logo));
            View.Add (sloganLabel = new UILabel () {
                Text = "WelcomeSlogan".Tr (),
            }.Apply (Style.Welcome.Slogan));
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
            Google.Plus.SignIn.SharedInstance.Authenticate ();
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            navController = NavigationController;
            if (navController != null) {
                navController.SetNavigationBarHidden (true, animated);
            }

            Google.Plus.SignIn.SharedInstance.Finished += OnGPlusFinished;
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            var tracker = ServiceContainer.Resolve<IGAITracker> ();
            tracker.Set (GAIConstants.ScreenName, "Welcome View");
            tracker.Send (GAIDictionaryBuilder.CreateAppView ().Build ());
        }

        public override void ViewWillDisappear (bool animated)
        {
            base.ViewWillDisappear (animated);

            if (navController != null) {
                navController.SetNavigationBarHidden (false, animated);
                navController = null;
            }

            Google.Plus.SignIn.SharedInstance.Finished -= OnGPlusFinished;
        }

        private bool IsAuthenticating {
            get { return !View.UserInteractionEnabled; }
            set {
                if (View.UserInteractionEnabled == !value)
                    return;

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

        private void OnGPlusFinished (object sender, Google.Plus.SignInDelegateFinishedEventArgs e)
        {
            InvokeOnMainThread (async delegate {
                try {
                    if (e.Error == null) {
                        IsAuthenticating = true;

                        var token = Google.Plus.SignIn.SharedInstance.Authentication.AccessToken;

                        var authManager = ServiceContainer.Resolve<AuthManager> ();
                        var success = await authManager.AuthenticateWithGoogle (token);

                        // No need to keep the users Google account access around anymore
                        Google.Plus.SignIn.SharedInstance.Disconnect ();

                        if (!success) {
                            new UIAlertView (
                                "WelcomeLoginErrorTitle".Tr (),
                                "WelcomeLoginErrorMessage".Tr (),
                                null, "WelcomeLoginErrorOk".Tr (), null).Show ();
                        }
                    } else {
                        new UIAlertView (
                            "WelcomeGoogleErrorTitle".Tr (),
                            "WelcomeGoogleErrorMessage".Tr (),
                            null, "WelcomeGoogleErrorOk".Tr (), null).Show ();
                    }
                } finally {
                    IsAuthenticating = false;
                }
            });
        }
    }
}
