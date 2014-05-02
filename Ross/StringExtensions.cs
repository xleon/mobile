using System;
using MonoTouch.Foundation;

namespace Toggl.Ross
{
    public static class StringExtensions
    {
        public static string Tr (this string key)
        {
            return NSBundle.MainBundle.LocalizedString (key, null, null);
        }
    }
}
