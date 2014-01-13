using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe
{
    public sealed class MessageBus
    {
        private readonly Dictionary<Type, List<WeakReference>> registry =
            new Dictionary<Type, List<WeakReference>> ();

        /// <summary>
        /// Subscribe listener to receive messages for TMessage. This method returns a subscription object, which you
        /// need to keep in scope for how long you want to the listener to be active. The moment the subscription object
        /// is garbage collected message delivery to listeners is not guaranteed anymore.
        /// </summary>
        /// <param name="listener">Listener.</param>
        /// <typeparam name="TMessage">Type of the message to subscribe to.</typeparam>
        /// <returns>A subscription object.</returns>
        public object Subscribe<TMessage> (Action<TMessage> listener)
            where TMessage : Message
        {
            if (listener == null)
                throw new ArgumentNullException ("listener");

            List<WeakReference> listeners;
            if (!registry.TryGetValue (typeof(TMessage), out listeners)) {
                listeners = new List<WeakReference> ();
                registry [typeof(TMessage)] = listeners;
            }
            listeners.Add (new WeakReference (listener));
            return listener;
        }

        /// <summary>
        /// Unsubscribes the specified subscription from receiving anymore messages.
        /// </summary>
        /// <param name="subscription">A subscription object from Subscribing to a message.</param>
        public void Unsubscribe (object subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException ("subscription");

            foreach (var listeners in registry.Values) {
                listeners.RemoveAll ((weak) => !weak.IsAlive || weak.Target == subscription);
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

            List<WeakReference> listeners;
            if (registry.TryGetValue (typeof(TMessage), out listeners)) {
                foreach (var weak in listeners.ToList()) {
                    var listener = weak.Target as Action<TMessage>;
                    if (listener != null) {
                        listener (msg);
                    } else {
                        listeners.Remove (weak);
                    }
                }
            }
        }
    }
}
