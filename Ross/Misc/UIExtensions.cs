using System.Linq;
using UIKit;

namespace Toggl.Ross
{
    static class UIExtensions
    {
        public static void PushViewControllers(this UINavigationController controller, bool animated, params UIViewController[] viewControllers)
        {
            controller.SetViewControllers(
                controller.ViewControllers
                .Concat(viewControllers.Where(vc => vc != null))
                .ToArray(),
                animated);
        }
    }
}

