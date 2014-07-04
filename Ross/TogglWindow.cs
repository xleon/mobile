using System.Drawing;
using System.Linq;
using MonoTouch.UIKit;

namespace Toggl.Ross
{
    public class TogglWindow : UIWindow
    {
        public TogglWindow (RectangleF bounds) : base (bounds)
        {

        }

        public override UIView HitTest (PointF point, UIEvent uievent)
        {
            var view = base.HitTest (point, uievent);
            if (OnHitTest != null)
                OnHitTest (view);
            return view;
        }
            
        public event OnHitTestHandler OnHitTest;

        public delegate void OnHitTestHandler (UIView view);
    }
}
