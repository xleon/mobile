using System;
using System.Drawing;
using MonoTouch.UIKit;

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

        public override RectangleF TextRect (RectangleF forBounds)
        {
            return base.TextRect (inset.InsetRect (forBounds));
        }

        public override RectangleF EditingRect (RectangleF forBounds)
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
