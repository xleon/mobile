using MonoTouch.UIKit;

namespace Toggl.Ross.Theme
{
    public static class UIExtensions
    {
        public static T ApplyStyle<T> (this T view, ViewStyler<T> style)
            where T : UIView
        {
            style (view);
            return view;
        }
    }
}
