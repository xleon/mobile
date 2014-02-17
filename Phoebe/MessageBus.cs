using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Toggl.Phoebe
{
    public sealed class MessageBus
    {
        private readonly int threadId;
        private readonly SynchronizationContext threadContext;
        private readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim ();
        private readonly Dictionary<Type, List<WeakReference>> registry =
            new Dictionary<Type, List<WeakReference>> ();
        private readonly Queue<Action> dispatchQueue = new Queue<Action> ();
        private volatile bool isScheduled;

        public MessageBus ()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
            threadContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Subscribe listener to receive messages for TMessage. This method returns a subscription object, which you
        /// need to keep in scope for how long you want to the listener to be active. The moment the subscription object
        /// is garbage collected message delivery to listeners is not guaranteed anymore.
        /// </summary>
        /// <param name="listener">Listener.</param>
        /// <param name="threadSafe">Indicates if the listener is thread-safe or not.</param>
        /// <typeparam name="TMessage">Type of the message to subscribe to.</typeparam>
        /// <returns>A subscription object, null if subscribing failed.</returns>
        public Subscription<TMessage> Subscribe<TMessage> (Action<TMessage> listener, bool threadSafe = false)
            where TMessage : Message
        {
            if (listener == null)
                throw new ArgumentNullException ("listener");

            var subscription = new Subscription<TMessage> (listener, threadSafe);

            rwlock.EnterWriteLock ();
            try {
                List<WeakReference> subscriptions;
                if (!registry.TryGetValue (typeof(TMessage), out subscriptions)) {
                    subscriptions = new List<WeakReference> ();
                    registry [typeof(TMessage)] = subscriptions;
                }
                subscriptions.Add (new WeakReference (subscription));
            } finally {
                rwlock.ExitWriteLock ();
            }

            return subscription;
        }

        /// <summary>
        /// Unsubscribes the specified subscription from receiving anymore messages.
        /// </summary>
        /// <param name="subscription">A subscription object from Subscribing to a message.</param>
        public void Unsubscribe<TMessage> (Subscription<TMessage> subscription)
            where TMessage : Message
        {
            rwlock.EnterWriteLock ();
            try {
                foreach (var listeners in registry.Values) {
                    listeners.RemoveAll ((weak) => !weak.IsAlive || weak.Target == subscription);
                }
            } finally {
                rwlock.ExitWriteLock ();
            }
        }

        /// <summary>
        /// Send the specified message.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <typeparam name="TMessage">Type of the message to send.</typeparam>
        public void Send<TMessage> (TMessage msg)
            where TMessage : Message
        {
            if (msg == null)
                throw new ArgumentNullException ("msg");

            List<Subscription<TMessage>> sendMain = null;
            List<Subscription<TMessage>> sendHere = null;
            var onMainThread = Thread.CurrentThread.ManagedThreadId == threadId;
            var needsPurge = false;

            // Process message:
            rwlock.EnterReadLock ();
            try {
                List<WeakReference> subscriptions;
                if (registry.TryGetValue (typeof(TMessage), out subscriptions)) {
                    foreach (var weak in subscriptions) {
                        var subscription = weak.Target as Subscription<TMessage>;
                        if (subscription != null) {
                            if (onMainThread || !subscription.IsThreadSafe) {
                                // Add the item to be called on the main thread
                                if (sendMain == null) {
                                    sendMain = new List<Subscription<TMessage>> ();
                                }
                                sendMain.Add (subscription);
                            } else {
                                // Add the item to be called on this thread
                                if (sendHere == null) {
                                    sendHere = new List<Subscription<TMessage>> ();
                                }
                                sendHere.Add (subscription);
                            }
                        } else {
                            needsPurge = true;
                        }
                    }
                }
            } finally {
                rwlock.ExitReadLock ();
            }

            // Dispatch messages (on this thread):
            if (sendHere != null) {
                foreach (var subscription in sendHere) {
                    subscription.Listener (msg);
                }
            }

            if (sendMain != null) {
                // Add to main thread dispatch queue:
                rwlock.EnterWriteLock ();
                try {
                    foreach (var subscription in sendMain) {
                        dispatchQueue.Enqueue (delegate {
                            subscription.Listener (msg);
                        });
                    }
                } finally {
                    rwlock.ExitWriteLock ();
                }

                // Make sure the queue is processed now or in the future
                if (onMainThread) {
                    ProcessQueue ();
                } else if (!isScheduled) {
                    ScheduleProcessQueue ();
                }
            }

            // Purge dead subscriptions
            if (needsPurge) {
                rwlock.EnterWriteLock ();
                try {
                    foreach (var listeners in registry.Values) {
                        listeners.RemoveAll ((weak) => !weak.IsAlive);
                    }
                } finally {
                    rwlock.ExitWriteLock ();
                }
            }
        }

        private void ScheduleProcessQueue ()
        {
            rwlock.EnterWriteLock ();
            try {
                if (!isScheduled) {
                    isScheduled = true;
                    threadContext.Post ((state) => {
                        ProcessQueue ();
                    }, null);
                }
            } finally {
                rwlock.ExitWriteLock ();
            }
        }

        private void ProcessQueue ()
        {
            while (true) {
                Action act;

                rwlock.EnterWriteLock ();
                try {
                    if (dispatchQueue.Count > 0) {
                        act = dispatchQueue.Dequeue ();
                    } else {
                        return;
                    }
                } finally {
                    rwlock.ExitWriteLock ();
                }

                // Need to execute the item outside of lock to prevent recursive locking
                act ();
            }
        }
    }
}
