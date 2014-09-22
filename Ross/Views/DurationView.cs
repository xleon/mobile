using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;

namespace Toggl.Ross.Views
{
    [Adopts ("UIKeyInput")]
    [Adopts ("UITextInputTraits")]
    public class DurationView : UILabel
    {
        private const float CursorWidth = 2f;
        private Duration hintDuration;
        private Duration duration;
        private int digitsEntered = 0;
        private UIView cursorView;
        private NSTimer cursorTimer;

        public DurationView ()
        {
            KeyboardAppearance = UIKeyboardAppearance.Default;
            UserInteractionEnabled = true;
            LineBreakMode = UILineBreakMode.Clip;
            UpdateText ();

            Add (cursorView = new UIView () {
                BackgroundColor = UIColor.Blue,
                Alpha = 0,
            });
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            var cursorHeight = Font.Ascender;
            cursorView.Frame = new RectangleF (
                Frame.Width - 7f - CursorWidth,
                (Frame.Height - cursorHeight) / 2,
                CursorWidth,
                cursorHeight
            );
        }

        public Duration Hint
        {
            get { return hintDuration; }
            set {
                if (hintDuration == value) {
                    return;
                }
                hintDuration = value;
                UpdateText ();
            }
        }

        public Duration EnteredDuration
        {
            get { return duration; }
        }

        public event EventHandler DurationChanged;

        private void UpdateText ()
        {
            var durationShown = digitsEntered > 0 ? duration : hintDuration;
            var durationText = String.Concat (" ", durationShown.ToString (), " ");

            var durationAttributed = new NSMutableAttributedString (durationText);
            if (digitsEntered > 0) {
                // Divider
                var sepIdx = durationText.IndexOf (":", StringComparison.Ordinal);
                if (sepIdx >= 0) {
                    durationAttributed.AddAttributes (
                    new UIStringAttributes () { ForegroundColor = HighlightedTextColor },
                    new NSRange (sepIdx, 1)
                    );
                }
                // Color entered minutes
                var minutesLength = Math.Min (digitsEntered, 2);
                durationAttributed.AddAttributes (
                new UIStringAttributes () { ForegroundColor = HighlightedTextColor },
                new NSRange (durationText.Length - 1 - minutesLength, minutesLength)
                );
                // Color entered hours
                if (digitsEntered > 2) {
                    var hoursLength = digitsEntered - 2;
                    durationAttributed.AddAttributes (
                    new UIStringAttributes () { ForegroundColor = HighlightedTextColor },
                    new NSRange (1 + 2 - hoursLength, hoursLength)
                    );
                }
            }
            AttributedText = durationAttributed;
        }

        protected void OnDurationChanged ()
        {
            UpdateText ();

            var cb = DurationChanged;
            if (cb != null) {
                cb (this, EventArgs.Empty);
            }
        }

        public override bool CanBecomeFirstResponder
        {
            get { return true; }
        }

        [Export ("autocapitalizationType")]
        UITextAutocapitalizationType AutocapitalizationType
        {
            get { return UITextAutocapitalizationType.None; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("autocorrectionType")]
        UITextAutocorrectionType AutocorrectionType
        {
            get { return UITextAutocorrectionType.No; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("enablesReturnKeyAutomatically")]
        bool EnablesReturnKeyAutomatically
        {
            get { return true; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("keyboardAppearance")]
        UIKeyboardAppearance KeyboardAppearance { get; set; }

        [Export ("keyboardType")]
        UIKeyboardType KeyboardType
        {
            get { return UIKeyboardType.NumberPad; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("returnKeyType")]
        UIReturnKeyType ReturnKeyType
        {
            get { return UIReturnKeyType.Default; }
            set { throw new InvalidOperationException (); }
        }

        [Export ("secureTextEntry")]
        bool SecureTextEntry
        {
            get { return false; }
            set { throw new InvalidOperationException (); }
        }

        public bool HasText
        {
            [Export ("hasText")]
            get { return digitsEntered > 0; }
        }

        [Export ("deleteBackward")]
        public void DeleteBackward ()
        {
            if (digitsEntered < 1) {
                return;
            }
            duration = duration.RemoveDigit ();
            if (duration == Duration.Zero) {
                // Clear all entered digits, is no value remains
                digitsEntered = 0;
            } else {
                digitsEntered -= 1;
            }

            OnDurationChanged ();
        }

        [Export ("insertText:")]
        public void InsertText (string text)
        {
            if (text == null) {
                throw new ArgumentNullException ("text");
            }
            if (digitsEntered > 3) {
                return;
            }

            int num;
            if (!Int32.TryParse (text, out num) || num < 0 || num > 9) {
                return;
            }

            duration = duration.AppendDigit (num);
            digitsEntered += 1;
            OnDurationChanged ();
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
                // Start blinking the cursor
                if (cursorTimer != null) {
                    cursorTimer.Invalidate ();
                    cursorTimer = null;
                }

                cursorView.Alpha = 0;
                UIView.Animate (0.5f, 0,
                                UIViewAnimationOptions.CurveEaseInOut
                                | UIViewAnimationOptions.Autoreverse
                                | UIViewAnimationOptions.AllowUserInteraction,
                delegate {
                    cursorView.Alpha = 1;
                },
                delegate {
                    cursorView.Alpha = 0;
                }
                               );

                cursorTimer = NSTimer.CreateRepeatingScheduledTimer (1.75f, delegate {
                    UIView.Animate (0.5f, 0,
                                    UIViewAnimationOptions.CurveEaseInOut
                                    | UIViewAnimationOptions.Autoreverse
                                    | UIViewAnimationOptions.AllowUserInteraction,
                    delegate {
                        cursorView.Alpha = 1;
                    },
                    delegate {
                        cursorView.Alpha = 0;
                    }
                                   );
                });

            }
            return res;
        }

        public override bool ResignFirstResponder ()
        {
            var res = base.ResignFirstResponder ();
            if (res) {
                // Stop blinking the cursor
                if (cursorTimer != null) {
                    cursorTimer.Invalidate ();
                    cursorTimer = null;
                }
            }
            return res;
        }
    }
}
