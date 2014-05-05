using System;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;
using Cirrious.FluentLayouts.Touch;

namespace Toggl.Ross.ViewControllers
{
    public class WelcomeViewController : UIViewController
    {
        private UIImageView logoImageView;
        private UILabel sloganLabel;
        private UIButton createButton;
        private UIButton passwordButton;
        private UIButton googleButton;

        public override void LoadView ()
        {
            View = new UIImageView () {
                UserInteractionEnabled = true,
            }.ApplyStyle (Style.Welcome.Background);
            View.Add (logoImageView = new UIImageView ().ApplyStyle (Style.Welcome.Logo));
            View.Add (sloganLabel = new UILabel () {
                Text = "WelcomeSlogan".Tr (),
            }.ApplyStyle (Style.Welcome.Slogan));
            View.Add (createButton = new UIButton ().ApplyStyle (Style.Welcome.CreateAccount));
            View.Add (passwordButton = new UIButton ().ApplyStyle (Style.Welcome.PasswordLogin));
            View.Add (googleButton = new UIButton ().ApplyStyle (Style.Welcome.GoogleLogin));

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
            Console.WriteLine ("Should login using google..");
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            var navController = NavigationController;
            if (navController != null) {
                navController.SetNavigationBarHidden (true, animated);
            }
        }

        public override void ViewWillDisappear (bool animated)
        {
            base.ViewWillDisappear (animated);

            var navController = NavigationController;
            if (navController != null) {
                navController.SetNavigationBarHidden (false, animated);
            }
        }
    }
}
