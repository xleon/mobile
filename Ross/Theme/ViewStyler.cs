using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public delegate void ViewStyler<in T> (T view)
        where T : UIView;
}
