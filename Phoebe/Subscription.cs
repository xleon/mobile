using System;

namespace Toggl.Phoebe
{
    public class Subscription<T>
        where T : Message
    {
        private readonly Action<T> listener;
        private readonly bool threadSafe;

        internal Subscription (Action<T> listener, bool threadSafe)
        {
            if (listener == null)
                throw new ArgumentNullException ("listener");
            this.listener = listener;
            this.threadSafe = threadSafe;
        }

        public Action<T> Listener {
            get { return listener; }
        }

        /// <summary>
        /// Gets a value indicating whether listener associated with this instance can be called from any thread
        /// or if it should be scheduled on the main thread.
        /// </summary>
        /// <value><c>true</c> if this instance is thread safe; otherwise, <c>false</c>.</value>
        public bool IsThreadSafe {
            get { return threadSafe; }
        }
    }
}

