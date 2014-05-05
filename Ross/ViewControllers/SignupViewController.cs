using System;
using Cirrious.FluentLayouts.Touch;
using MonoTouch.UIKit;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class SignupViewController : UIViewController
    {
        private UIView inputsContainer;
        private UIView topBorder;
        private UIView middleBorder;
        private UIView bottomBorder;
        private UITextField emailTextField;
        private UITextField passwordTextField;
        private UIButton passwordActionButton;
        private UIButton googleActionButton;

        public SignupViewController ()
        {
            Title = "SignupTitle".Tr ();
        }

        public override void LoadView ()
        {
            View = new UIView ()
                .ApplyStyle (Style.Screen);

            View.Add (inputsContainer = new UIView ().ApplyStyle (Style.Signup.InputsContainer));

            inputsContainer.Add (topBorder = new UIView ().ApplyStyle (Style.Signup.InputsBorder));

            inputsContainer.Add (emailTextField = new UITextField () {
                Placeholder = "SignupEmailHint".Tr (),
                AutocapitalizationType = UITextAutocapitalizationType.None,
                KeyboardType = UIKeyboardType.EmailAddress,
                ReturnKeyType = UIReturnKeyType.Next,
                ClearButtonMode = UITextFieldViewMode.Always,
                ShouldReturn = HandleShouldReturn,
            }.ApplyStyle (Style.Signup.EmailField));

            inputsContainer.Add (middleBorder = new UIView ().ApplyStyle (Style.Signup.InputsBorder));

            inputsContainer.Add (passwordTextField = new UITextField () {
                Placeholder = "SignupPasswordHint".Tr (),
                AutocapitalizationType = UITextAutocapitalizationType.None,
                AutocorrectionType = UITextAutocorrectionType.No,
                SecureTextEntry = true,
                ReturnKeyType = UIReturnKeyType.Go,
                ShouldReturn = HandleShouldReturn,
            }.ApplyStyle (Style.Signup.PasswordField));

            inputsContainer.Add (bottomBorder = new UIView ().ApplyStyle (Style.Signup.InputsBorder));

            View.Add (passwordActionButton = new UIButton ()
                .ApplyStyle (Style.Signup.SignupButton));
            passwordActionButton.SetTitle ("SignupSignupButtonText".Tr (), UIControlState.Normal);
            passwordActionButton.TouchUpInside += OnPasswordActionButtonTouchUpInside;

            View.Add (googleActionButton = new UIButton ()
                .ApplyStyle (Style.Signup.GoogleButton));
            googleActionButton.SetTitle ("SignupGoogleButtonText".Tr (), UIControlState.Normal);
            googleActionButton.TouchUpInside += OnGoogleActionButtonTouchUpInside;

            inputsContainer.AddConstraints (
                topBorder.AtTopOf (inputsContainer),
                topBorder.AtLeftOf (inputsContainer),
                topBorder.AtRightOf (inputsContainer),
                topBorder.Height ().EqualTo (1f),

                emailTextField.Below (topBorder),
                emailTextField.AtLeftOf (inputsContainer, 20f),
                emailTextField.AtRightOf (inputsContainer, 10f),
                emailTextField.Height ().EqualTo (42f),

                middleBorder.Below (emailTextField),
                middleBorder.AtLeftOf (inputsContainer, 20f),
                middleBorder.AtRightOf (inputsContainer),
                middleBorder.Height ().EqualTo (1f),

                passwordTextField.Below (middleBorder),
                passwordTextField.AtLeftOf (inputsContainer, 20f),
                passwordTextField.AtRightOf (inputsContainer),
                passwordTextField.Height ().EqualTo (42f),

                bottomBorder.Below (passwordTextField),
                bottomBorder.AtLeftOf (inputsContainer),
                bottomBorder.AtRightOf (inputsContainer),
                bottomBorder.AtBottomOf (inputsContainer),
                bottomBorder.Height ().EqualTo (1f)
            );

            inputsContainer.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();

            View.AddConstraints (
                inputsContainer.AtTopOf (View, 80f),
                inputsContainer.AtLeftOf (View),
                inputsContainer.AtRightOf (View),

                passwordActionButton.Below (inputsContainer, 20f),
                passwordActionButton.AtLeftOf (View),
                passwordActionButton.AtRightOf (View),
                passwordActionButton.Height ().EqualTo (60f)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();
        }

        private bool HandleShouldReturn (UITextField textField)
        {
            if (textField == emailTextField) {
                passwordTextField.BecomeFirstResponder ();
            } else if (textField == passwordTextField) {
                textField.ResignFirstResponder ();
                TryPasswordSignup ();
            } else {
                return false;
            }
            return true;
        }

        private void OnPasswordActionButtonTouchUpInside (object sender, EventArgs e)
        {
            TryPasswordSignup ();
        }

        private void OnGoogleActionButtonTouchUpInside (object sender, EventArgs e)
        {
            // TODO: Add Google signup
            Console.WriteLine ("Should attempt to sign up with G+...");
        }

        private async void TryPasswordSignup ()
        {
            if (IsAuthenticating)
                return;

            IsAuthenticating = true;

            try {
                var authManager = ServiceContainer.Resolve<AuthManager> ();
                var success = await authManager.Signup (emailTextField.Text, passwordTextField.Text);

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

                passwordActionButton.SetTitle ("SignupSignupProgressText".Tr (), UIControlState.Disabled);
            }
        }
    }
}
