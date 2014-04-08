using Cirrious.FluentLayouts.Touch;
using MonoTouch.UIKit;
using PixateFreestyleLib;

namespace Toggl.Ross.ViewControllers
{
    public class LoginViewController : UIViewController
    {
        private UILabel headerLabel;
        private UITextField emailTextField;
        private UITextField passwordTextField;
        private UIButton passwordActionButton;
        private UIButton googleActionButton;

        public override void LoadView ()
        {
            View = new UIView ();
            View.SetStyleId ("loginView");
            View.AddStyleClass ("screen");

            View.Add (headerLabel = new UILabel () {
                Text = "LoginHeaderText".Tr (),
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
            passwordActionButton.SetStyleId ("passwordLoginButton");

            View.Add (googleActionButton = new UIButton ());
            googleActionButton.SetTitle ("LoginGoogleButtonText".Tr (), UIControlState.Normal);
            googleActionButton.SetStyleId ("googleLoginButton");

            View.AddConstraints (
                headerLabel.AtTopOf (View, 50f),
                headerLabel.WithSameCenterX (View),

                emailTextField.Below (headerLabel, 40f),
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
            } else {
                return false;
            }
            return true;
        }
    }
}
