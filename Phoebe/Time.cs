using System;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static class Time
    {
        private static ITimeProvider Provider {
            get { return ServiceContainer.Resolve<ITimeProvider> (); }
        }

        public static DateTime Now {
            get { return Provider.Now; }
        }

        public static DateTime UtcNow {
            get { return Provider.UtcNow; }
        }

        public static string TimeZoneId {
            get { return Provider.TimeZoneId; }
        }
    }
}
