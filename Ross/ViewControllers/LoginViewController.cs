using System;
using Cirrious.FluentLayouts.Touch;
using MonoTouch.UIKit;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class LoginViewController : UIViewController
    {
        private UILabel headerLabel;
        private UISegmentedControl actionSegmentedControl;
        private UITextField emailTextField;
        private UITextField passwordTextField;
        private UIButton passwordActionButton;
        private UIButton googleActionButton;

        public override void LoadView ()
        {
            View = new UIView ();

            View.Add (headerLabel = new UILabel () {
                Text = "LoginHeaderText".Tr (),
            });

            View.Add (actionSegmentedControl = new UISegmentedControl (new object[] {
                "LoginLoginTabText".Tr (),
                "LoginSignupTabText".Tr (),
            }) {
                SelectedSegment = 0,
            });

            View.Add (emailTextField = new UITextField () {
                Placeholder = "LoginEmailHint".Tr (),
                AutocapitalizationType = UITextAutocapitalizationType.None,
                KeyboardType = UIKeyboardType.EmailAddress,
                ReturnKeyType = UIReturnKeyType.Next,
                ClearButtonMode = UITextFieldViewMode.Always,
                ShouldReturn = HandleShouldReturn,
            });

            View.Add (passwordTextField = new UITextField () {
                Placeholder = "LoginPasswordHint".Tr (),
                AutocapitalizationType = UITextAutocapitalizationType.None,
                AutocorrectionType = UITextAutocorrectionType.No,
                SecureTextEntry = true,
                ReturnKeyType = UIReturnKeyType.Go,
                ShouldReturn = HandleShouldReturn,
            });

            View.Add (passwordActionButton = new UIButton ());
            passwordActionButton.SetTitle ("LoginLoginButtonText".Tr (), UIControlState.Normal);
            passwordActionButton.TouchUpInside += OnPasswordActionButtonTouchUpInside;

            View.Add (googleActionButton = new UIButton ());
            googleActionButton.SetTitle ("LoginGoogleButtonText".Tr (), UIControlState.Normal);

            View.AddConstraints (
                headerLabel.AtTopOf (View, 80f),
                headerLabel.WithSameCenterX (View),

                actionSegmentedControl.Below (headerLabel, 40f),
                actionSegmentedControl.AtLeftOf (View, 10f),
                actionSegmentedControl.AtRightOf (View, 10f),

                emailTextField.Below (actionSegmentedControl, 20f),
                emailTextField.AtLeftOf (View, 10f),
                emailTextField.AtRightOf (View, 10f),

                passwordTextField.Below (emailTextField, 10f),
                passwordTextField.AtLeftOf (View, 10f),
                passwordTextField.AtRightOf (View, 10f),

                passwordActionButton.Below (passwordTextField, 10f),
                passwordActionButton.AtLeftOf (View, 10f),
                passwordActionButton.AtRightOf (View, 10f),

                googleActionButton.AtBottomOf (View, 10f),
                googleActionButton.AtLeftOf (View, 10f),
                googleActionButton.AtRightOf (View, 10f)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();
        }

        private bool HandleShouldReturn (UITextField textField)
        {
            if (textField == emailTextField) {
                passwordTextField.BecomeFirstResponder ();
            } else if (textField == passwordTextField) {
                textField.ResignFirstResponder ();

                // TODO: Start login
                TryPasswordAuth ();
            } else {
                return false;
            }
            return true;
        }

        private void OnPasswordActionButtonTouchUpInside (object sender, EventArgs e)
        {
            TryPasswordAuth ();
        }

        private async void TryPasswordAuth ()
        {
            if (IsAuthenticating)
                return;

            IsAuthenticating = true;

            try {
                var authManager = ServiceContainer.Resolve<AuthManager> ();
                await System.Threading.Tasks.Task.Delay (TimeSpan.FromSeconds (5));
                var success = await authManager.Authenticate (emailTextField.Text, passwordTextField.Text);

                if (!success) {
                    // TODO: Show error
                }
            } finally {
                IsAuthenticating = false;
            }
        }

        private bool isAuthenticating;

        private bool IsAuthenticating {
            get { return isAuthenticating; }
            set {
                isAuthenticating = value;
                emailTextField.Enabled = !isAuthenticating;
                passwordTextField.Enabled = !isAuthenticating;
                passwordActionButton.Enabled = !isAuthenticating;
                googleActionButton.Enabled = !isAuthenticating;

                passwordActionButton.SetTitle ("LoginLoginProgressText".Tr (), UIControlState.Disabled);
            }
        }
    }
}
