using System;
using MonoTouch.Foundation;
using Toggl.Phoebe;

namespace Toggl.Ross
{
    public class NSTimeProvider : ITimeProvider
    {
        public DateTime Now {
            get { return DateTime.Now; }
        }

        public DateTime UtcNow {
            get { return DateTime.UtcNow; }
        }

        public string TimeZoneId {
            get { return NSTimeZone.SystemTimeZone.Name; }
        }
    }
}
