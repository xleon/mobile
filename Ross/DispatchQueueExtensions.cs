using System;
using CoreFoundation;
using Foundation;

namespace Toggl.Ross
{
    public static class DispatchQueueExtensions
    {
        public static void DispatchAfter(this DispatchQueue self, TimeSpan delay, Action action)
        {
            var time = new DispatchTime(DispatchTime.Now, delay.Ticks * 100);
            self.DispatchAfter(time, action);
        }
    }
}
