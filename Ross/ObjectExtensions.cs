using System;

namespace Toggl.Ross
{
    public static class ObjectExtensions
    {
        public static T Apply<T> (this T self, Action<T> extension)
            where T : class
        {
            extension (self);
            return self;
        }
    }
}
