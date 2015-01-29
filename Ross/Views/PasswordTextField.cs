using CoreGraphics;
using Foundation;
using UIKit;

namespace Toggl.Ross.Views
{
    public sealed class PasswordTextField : UITextField
    {
        private const string ShowText = "\U0000E601";
        private const string HideText = "\U0000E600";
        private readonly UIFont buttonFont = UIFont.FromName ("icomoon", 14f);

        public PasswordTextField()
        {
            var obfuscateButton = new UIButton (GetButtonRect());
            obfuscateButton.SetTitle (ShowText, UIControlState.Normal);
            obfuscateButton.SetTitleColor (UIColor.LightGray, UIControlState.Normal);
            obfuscateButton.Font = buttonFont;
            obfuscateButton.TouchUpInside += (sender, args) => {
                SecureTextEntry = !SecureTextEntry;
                Text = Text; //need this to address cursor position update issue
                obfuscateButton.SetTitle (SecureTextEntry ? ShowText : HideText, UIControlState.Normal);
            };
            RightViewMode = UITextFieldViewMode.WhileEditing;
            RightView = obfuscateButton;
        }

        public override CGRect RightViewRect (CGRect forBounds)
        {
            return GetButtonRect();
        }

        public override CGRect EditingRect (CGRect forBounds)
        {
            var rect = base.EditingRect (forBounds);
            rect.Width -= GetButtonSize().Width;
            return rect;
        }

        private CGRect GetButtonRect()
        {
            var size = GetButtonSize();
            var x = Frame.Width - size.Width - 15 /* margin right */;
            var y = (Frame.Height - size.Height)/2;
            return new CGRect (x, y, size.Width, size.Height);
        }

        private CGSize GetButtonSize()
        {
            var attr = new UIStringAttributes {Font = buttonFont};
            var size1 = ((NSString) ShowText).GetSizeUsingAttributes (attr);
            var size2 = ((NSString) HideText).GetSizeUsingAttributes (attr);
            return size1.Width > size2.Width ? size1 : size2;
        }
    }
}