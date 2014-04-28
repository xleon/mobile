using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class MainViewController : UINavigationController
    {
        public MainViewController () : base (typeof(TallerNavigationBar), null)
        {
            UIViewController activeController;
            activeController = new LogViewController ();
            ViewControllers = new UIViewController[] { activeController };
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            NavigationBar.ApplyStyle (Style.NavigationBar);
        }

        private class TallerNavigationBar : UINavigationBar
        {
            public TallerNavigationBar (IntPtr handle) : base (handle)
            {
            }

            const float AdditionalHeight = 20f;

            public override SizeF SizeThatFits (SizeF size)
            {
                var s = base.SizeThatFits (size);
                s.Height += AdditionalHeight;
                return s;
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                foreach (var child in Subviews) {
                    var frame = child.Frame;
                    frame.Y -= AdditionalHeight / 2;
                    child.Frame = frame;
                }
            }
        }
    }
}
