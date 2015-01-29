using System;
using CoreGraphics;
using UIKit;

namespace Toggl.Ross.Views
{
    public class TextField : UITextField
    {
        private UIEdgeInsets inset;

        public TextField ()
        {
        }

        public TextField (IntPtr handle) : base (handle)
        {
        }

        public override CGRect TextRect (CGRect forBounds)
        {
            return base.TextRect (inset.InsetRect (forBounds));
        }

        public override CGRect EditingRect (CGRect forBounds)
        {
            return base.EditingRect (inset.InsetRect (forBounds));
        }

        public UIEdgeInsets TextEdgeInsets
        {
            get { return inset; }
            set {
                if (inset.Equals (value)) {
                    return;
                }
                inset = value;
                SetNeedsLayout ();
            }
        }
    }
}
