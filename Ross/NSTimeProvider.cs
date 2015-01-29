using System;
using Foundation;
using Toggl.Phoebe;
using XPlatUtils;

namespace Toggl.Ross
{
    public class NSTimeProvider : ITimeProvider
    {
        private static TimeSpan Correction
        {
            get { return ServiceContainer.Resolve<TimeCorrectionManager> ().Correction; }
        }

        public DateTime Now
        {
            get { return DateTime.Now + Correction; }
        }

        public DateTime UtcNow
        {
            get { return DateTime.UtcNow + Correction; }
        }

        public string TimeZoneId
        {
            get { return NSTimeZone.SystemTimeZone.Name; }
        }
    }
}
