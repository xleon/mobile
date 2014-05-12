using System;
using MonoTouch.Foundation;
using Toggl.Phoebe;

namespace Toggl.Ross
{
    public static class DateTimeExtensions
    {
        public static string ToLocalizedDateString (this DateTime self)
        {
            var date = self.ToLocalTime ().Date;
            var today = Time.Now.Date;
            if (date.Date == today) {
                return "DateTimeToday".Tr ();
            }
            if (date.Date == today - TimeSpan.FromDays (1)) {
                return "DateTimeYesterday".Tr ();
            }
            if (date.Year == today.Year) {
                return date.ToString ("DateTimeShortFormat".Tr ());
            }
            return date.ToString ("DateTimeLongFormat".Tr ());
        }

        public static string ToLocalizedTimeString (this DateTime self)
        {
            var fmt = new NSDateFormatter () {
                DateStyle = NSDateFormatterStyle.None,
                TimeStyle = NSDateFormatterStyle.Short,
            };
            return fmt.ToString (self.ToNSDate ());
        }

        public static NSDate ToNSDate (this DateTime self)
        {
            return NSDate.FromTimeIntervalSince1970 (self.ToUnix ().TotalSeconds);
        }
    }
}
