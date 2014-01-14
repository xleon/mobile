using System;

namespace Toggl.Phoebe
{
    public static class DateTimeExtensions
    {
        public static DateTime? ToUtc (this DateTime? val)
        {
            if (val == null)
                return null;
            return val.Value.ToUtc ();
        }

        public static DateTime ToUtc (this DateTime val)
        {
            if (val.Kind == DateTimeKind.Unspecified) {
                return DateTime.SpecifyKind (val, DateTimeKind.Utc);
            } else {
                return val.ToUniversalTime ();
            }
        }
    }
}

