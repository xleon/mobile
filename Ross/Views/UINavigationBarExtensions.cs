using System;
using UIKit;

namespace Toggl.Ross.Views
{
    public static class UINavigationBarExtensions
    {
        public static UIImageView GetBorderImage (this UINavigationBar navigationBar)
        {
            UIImageView borderImage = null;
            foreach (var view in navigationBar.TraverseTree()) {
                borderImage = view as UIImageView;
                if (borderImage == null) {
                    continue;
                }

                if (borderImage.Bounds.Size.Height <= 1.0) {
                    break;
                }

                borderImage = null;
            }
            return borderImage;
        }

    }
}

