using System;
using Android.Content;
using Android.Text.Format;
using Toggl.Phoebe;
using XPlatUtils;

namespace Toggl.Joey.UI.Utils
{
    public static class DateTimeExtensions
    {
        public static string ToDeviceTimeString(this DateTime self)
        {
            if (DateFormat.Is24HourFormat(ServiceContainer.Resolve<Context> ()))
            {
                return self.ToString("HH:mm");
            }
            return self.ToString("h:mm tt");
        }

        public static string ToDeviceDateString(this DateTime self)
        {
            var javaDate = new Java.Util.Date((long)self.ToUnix().TotalMilliseconds);
            return self.DayOfWeek.ToString().Substring(0, 3) + " " + DateFormat.GetDateFormat(ServiceContainer.Resolve<Context> ()).Format(javaDate);
        }
    }
}

