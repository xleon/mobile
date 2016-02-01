using System;
using System.Reactive.Concurrency;

namespace Toggl.Phoebe._Reactive
{
    public interface ISchedulerProvider
    {
        IScheduler GetScheduler ();
    }

    public class DefaultSchedulerProvider : ISchedulerProvider
    {
        public IScheduler GetScheduler ()
        {
            return Scheduler.Default;
        }
    }

    public class TestSchedulerProvider : ISchedulerProvider
    {
        public IScheduler GetScheduler ()
        {
            return Scheduler.CurrentThread;
        }
    }
}

