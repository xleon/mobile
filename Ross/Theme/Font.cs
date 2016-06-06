using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static class Font
    {
        public static UIFont Main(float height) => UIFont.FromName("SFUIText-Regular", height);
        public static UIFont MainLight(float height) => UIFont.FromName("SFUIText-Light", height);

        public static UIFont WithMonospacedDigits(this UIFont font)
        {
            var descriptor = font.FontDescriptor;

            var feature = new UIFontFeature(CoreText.CTFontFeatureNumberSpacing.Selector.MonospacedNumbers);

            var attribute = new UIFontAttributes(feature);

            var d = descriptor.CreateWithAttributes(attribute);

            return UIFont.FromDescriptor(d, font.FontDescriptor.Size.Value);
        }
    }
}

