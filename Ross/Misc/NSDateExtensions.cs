using System;
using Foundation;

namespace Toggl.Ross
{
    public static class NSDateExtensions
    {
        private static readonly DateTime ReferenceDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ToDateTime(this NSDate self)
        {
            return ReferenceDate + TimeSpan.FromSeconds(self.SecondsSinceReferenceDate);
        }
    }
}

