using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Toggl.Phoebe
{
    public sealed class MessageBus
    {
        private readonly ReaderWriterLock rwlock = new ReaderWriterLock ();
        private readonly Dictionary<Type, List<WeakReference>> registry =
            new Dictionary<Type, List<WeakReference>> ();

        private bool TryWrite (Action act)
        {
            try {
                rwlock.AcquireWriterLock (TimeSpan.FromMinutes (250));
                try {
                    act ();
                } finally {
                    rwlock.ReleaseWriterLock ();
                }
            } catch (ApplicationException) {
                return false;
            }
            return true;
        }

        private bool TryRead (Action act)
        {
            try {
                rwlock.AcquireReaderLock (TimeSpan.FromMinutes (250));
                try {
                    act ();
                } finally {
                    rwlock.ReleaseReaderLock ();
                }
            } catch (ApplicationException) {
                return false;
            }
            return true;
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

            var ok = TryWrite (delegate {
                List<WeakReference> subscriptions;
                if (!registry.TryGetValue (typeof(TMessage), out subscriptions)) {
                    subscriptions = new List<WeakReference> ();
                    registry [typeof(TMessage)] = subscriptions;
                }
                subscriptions.Add (new WeakReference (subscription));
            });

            if (!ok) {
                return null;
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
            TryWrite (delegate {
                foreach (var listeners in registry.Values) {
                    listeners.RemoveAll ((weak) => !weak.IsAlive || weak.Target == subscription);
                }
            });
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

            var needsPurge = false;

            // Dispatch message
            TryRead (delegate {
                List<WeakReference> subscriptions;
                if (registry.TryGetValue (typeof(TMessage), out subscriptions)) {
                    foreach (var weak in subscriptions) {
                        var subscription = weak.Target as Subscription<TMessage>;
                        if (subscription != null) {
                            subscription.Listener (msg);
                        } else {
                            needsPurge = true;
                        }
                    }
                }
            });

            // Purge dead subscriptions
            if (needsPurge) {
                TryWrite (delegate {
                    foreach (var listeners in registry.Values) {
                        listeners.RemoveAll ((weak) => !weak.IsAlive);
                    }
                });
            }
        }
    }
}
