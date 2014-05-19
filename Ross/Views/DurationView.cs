using System;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;

namespace Toggl.Ross.Views
{
    [Adopts ("UIKeyInput")]
    [Adopts ("UITextInputTraits")]
    public class DurationView : UILabel
    {
        public DurationView ()
        {
            KeyboardAppearance = UIKeyboardAppearance.Default;
            UserInteractionEnabled = true;
        }

        public override bool CanBecomeFirstResponder {
            get { return true; }
        }

        [Export ("autocapitalizationType")]
        UITextAutocapitalizationType AutocapitalizationType {
            get { return UITextAutocapitalizationType.None; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("autocorrectionType")]
        UITextAutocorrectionType AutocorrectionType {
            get { return UITextAutocorrectionType.No; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("enablesReturnKeyAutomatically")]
        bool EnablesReturnKeyAutomatically {
            get { return true; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("keyboardAppearance")]
        UIKeyboardAppearance KeyboardAppearance { get; set; }

        [Export ("keyboardType")]
        UIKeyboardType KeyboardType {
            get { return UIKeyboardType.NumberPad; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("returnKeyType")]
        UIReturnKeyType ReturnKeyType {
            get { return UIReturnKeyType.Default; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("secureTextEntry")]
        bool SecureTextEntry {
            get { return false; }
            set { throw new InvalidOperationException (); }
        }

        public bool HasText {
            [Export ("hasText")]
            get {
                UIApplication.EnsureUIThread ();
                // TODO:
                return false;
            }
        }

        [Export ("deleteBackward")]
        public void DeleteBackward ()
        {
            UIApplication.EnsureUIThread ();
            // TODO:
        }

        [Export ("insertText:")]
        public void InsertText (string text)
        {
            UIApplication.EnsureUIThread ();
            if (text == null) {
                throw new ArgumentNullException ("text");
            }
            // TODO:
        }

        public override void TouchesEnded (NSSet touches, UIEvent evt)
        {
            base.TouchesBegan (touches, evt);

            var touch = touches.AnyObject as UITouch;
            if (touch != null && touch.View == this) {
                BecomeFirstResponder ();
            }
        }

        public override bool BecomeFirstResponder ()
        {
            var res = base.BecomeFirstResponder ();
            if (res) {
                // TODO: Switch to edit mode
            }
            return res;
        }

        public override bool ResignFirstResponder ()
        {
            var res = base.ResignFirstResponder ();
            if (res) {
                // TODO: Switch back to label mode
            }
            return res;
        }
    }
}
