using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static class Font
    {
        public static UIFont Main(float height) => UIFont.FromName("HelveticaNeue", height);
        public static UIFont MainLight(float height) => UIFont.FromName("HelveticaNeue-Thin", height);
    }
}

