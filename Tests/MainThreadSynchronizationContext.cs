using System;
using System.Collections.Generic;
using System.Threading;

namespace Toggl.Phoebe.Tests
{
    public class MainThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Thread thread;
        private readonly object syncRoot = new Object ();
        private readonly Queue<Job> jobs = new Queue<Job> ();

        public MainThreadSynchronizationContext ()
        {
            thread = Thread.CurrentThread;
        }

        public bool Run ()
        {
            int count;
            Job job;

            lock (syncRoot) {
                count = jobs.Count;
                if (count < 1)
                    return false;
                job = jobs.Dequeue ();
            }

            try {
                job.Callback (job.State);
            } finally {
                if (job.OnProcessed != null) {
                    job.OnProcessed ();
                }
            }

            return count > 1;
        }

        public override void Post (SendOrPostCallback d, object state)
        {
            Post (d, state, null);
        }

        public override void Send (SendOrPostCallback d, object state)
        {
            if (thread == Thread.CurrentThread) {
                d (state);
            } else {
                // Schedule task and wait for it to complete
                var reset = new ManualResetEventSlim ();
                Post (d, state, reset.Set);
                reset.Wait ();
            }
        }

        private void Post (SendOrPostCallback d, object state, Action completionHandler)
        {
            lock (syncRoot) {
                jobs.Enqueue (new Job () {
                    Callback = d,
                    State = state,
                    OnProcessed = completionHandler,
                });
            }
        }

        struct Job
        {
            public SendOrPostCallback Callback;
            public object State;
            public Action OnProcessed;
        }
    }
}
