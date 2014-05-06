using System;

namespace Toggl.Phoebe
{
    public class DefaultTimeProvider : ITimeProvider
    {
        public DateTime Now {
            get { return DateTime.Now; }
        }

        public DateTime UtcNow {
            get { return DateTime.UtcNow; }
        }

        public string TimeZoneId {
            get {
                var tz = TimeZoneInfo.Local;
                return tz == null ? "UTC" : tz.Id;
            }
        }
    }
}
