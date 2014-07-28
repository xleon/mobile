using System;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public class DefaultTimeProvider : ITimeProvider
    {
        private static TimeSpan Correction {
            get { return ServiceContainer.Resolve<TimeCorrectionManager> ().Correction; }
        }

        public DateTime Now {
            get { return DateTime.Now + Correction; }
        }

        public DateTime UtcNow {
            get { return DateTime.UtcNow + Correction; }
        }

        public string TimeZoneId {
            get {
                var tz = TimeZoneInfo.Local;
                return tz == null ? "UTC" : tz.Id;
            }
        }
    }
}
