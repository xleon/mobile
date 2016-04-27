using CoreGraphics;
using UIKit;

namespace Toggl.Ross
{
    public class TogglWindow : UIWindow
    {
        public TogglWindow(CGRect bounds) : base(bounds)
        {
        }

        public override UIView HitTest(CGPoint point, UIEvent uievent)
        {
            var view = base.HitTest(point, uievent);
            if (OnHitTest != null)
            {
                OnHitTest(view);
            }
            return view;
        }

        public event OnHitTestHandler OnHitTest;

        public delegate void OnHitTestHandler(UIView view);
    }
}
