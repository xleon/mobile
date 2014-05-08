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
            // TODO: Check year
            return date.ToString ("MMMM d");
        }
    }
}
