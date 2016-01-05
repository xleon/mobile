using System;
using System.Threading.Tasks;

namespace Toggl.Phoebe
{
    public static class Util
    {
        public static Task<bool> AwaitPredicate (Func<bool> predicate, double interval = 100, double timeout = 5000)
        {
            var tcs = new TaskCompletionSource<bool> ();

            double timePassed = 0;
            var timer = new System.Timers.Timer (interval)  { AutoReset = true };
            timer.Elapsed += (s, e) => {
                timePassed += interval;
                if (timePassed >= timeout) {
                    timer.Stop ();
                    tcs.SetResult (false);
                } else {
                    var success = predicate ();
                    if (success) {
                        timer.Stop ();
                        tcs.SetResult (true);
                    }
                }
            };
            timer.Start ();

            return tcs.Task;
        }
    }
}
