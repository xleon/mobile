using System;
using CoreGraphics;
using System.Globalization;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static class UIColorExtensions
    {
        public static UIColor FromHex(this UIColor color, string hexValue, float alpha = 1f)
        {
            hexValue = hexValue.TrimStart('#');

            int rgb;
            if (!Int32.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
            {
                throw new ArgumentException("Invalid hex string.", "hexValue");
            }

            switch (hexValue.Length)
            {
                case 6:
                    return new UIColor(
                               ((rgb & 0xFF0000) >> 16) / 255.0f,
                               ((rgb & 0x00FF00) >> 8) / 255.0f,
                               (rgb & 0x0000FF) / 255.0f,
                               alpha
                           );
                case 3:
                    return new UIColor(
                               (((rgb & 0xF00) >> 4) | ((rgb & 0xF00) >> 8)) / 255.0f,
                               ((rgb & 0x0F0) | (rgb & 0x0F0) >> 4) / 255.0f,
                               ((rgb & 0x00F << 4) | (rgb & 0x00F)) / 255.0f,
                               alpha
                           );
                default:
                    throw new ArgumentException("Invalid hex string.", "hexValue");
            }
        }

        public static UIImage ToImage(this UIColor color)
        {
            var size = new CGSize(1f, 1f);

            UIGraphics.BeginImageContext(size);
            var ctx = UIGraphics.GetCurrentContext();

            ctx.SetFillColor(color.CGColor);
            ctx.FillRect(new CGRect(CGPoint.Empty, size));

            var image = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            return image;
        }
    }
}

