using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public sealed class TagChipCache
    {
        private readonly Dictionary<string, WeakReference<UIImage>> cache = new Dictionary<string, WeakReference<UIImage>> ();

        public UIImage Get (string tag, UIView hostView)
        {
            WeakReference<UIImage> weakImage;
            if (!cache.TryGetValue (tag, out weakImage)) {
                weakImage = new WeakReference<UIImage> (null);
                cache.Add (tag, weakImage);
            }

            UIImage image;
            if (!weakImage.TryGetTarget (out image)) {
                image = CreateImage ((NSString)tag, GetScale (hostView));
                weakImage.SetTarget (image);
            }

            return image;
        }

        private static float GetScale (UIView hostView)
        {
            var scale = 0f;
            if (hostView.Window != null && hostView.Window.Screen != null) {
                scale = hostView.Window.Screen.Scale;
            } else {
                scale = UIApplication.SharedApplication.KeyWindow.Screen.Scale;
            }
            return scale;
        }

        private UIImage CreateImage (NSString title, float scale)
        {
            var titleAttrs = new UIStringAttributes () {
                Font = UIFont.FromName ("HelveticaNeue", 13f),
                ForegroundColor = Color.Gray,
            };

            var titleBounds = title.GetBoundingRect (
                                  new SizeF (Single.PositiveInfinity, Single.PositiveInfinity),
                                  NSStringDrawingOptions.UsesFontLeading | NSStringDrawingOptions.UsesDeviceMetrics,
                                  titleAttrs,
                                  null);

            var image = Image.TagBackground;
            var imageBounds = new RectangleF (
                                  0, 0,
                                  (float)Math.Ceiling (titleBounds.Width) + image.CapInsets.Left + image.CapInsets.Right + 4f,
                                  (float)Math.Ceiling (titleBounds.Height) + image.CapInsets.Top + image.CapInsets.Bottom
                              );

            titleBounds.X = image.CapInsets.Left + 2f;
            titleBounds.Y = image.CapInsets.Top;

            UIGraphics.BeginImageContextWithOptions (imageBounds.Size, false, scale);

            try {
                image.Draw (imageBounds);
                title.DrawString (titleBounds, titleAttrs);
                return UIGraphics.GetImageFromCurrentImageContext ();
            } finally {
                UIGraphics.EndImageContext ();
            }
        }
    }
}
