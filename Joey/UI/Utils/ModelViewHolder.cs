using System;
using Android.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Joey.UI.Utils
{
    /// <summary>
    /// Model view holder takes care of automatically subscribing and unsubscribing to ModelChangedMessage.
    /// </summary>
    public abstract class ModelViewHolder<T> : BindableViewHolder<T>
    {
        private Subscription<ModelChangedMessage> subscriptionModelChanged;

        public ModelViewHolder (View root) : base (root)
        {
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                Unsubscribe (ServiceContainer.Resolve<MessageBus> ());
            }

            base.Dispose (disposing);
        }

        protected override void OnRootAttachedToWindow (object sender, View.ViewAttachedToWindowEventArgs e)
        {
            base.OnRootAttachedToWindow (sender, e);
            Subscribe (ServiceContainer.Resolve<MessageBus> ());
        }

        protected override void OnRootDetachedFromWindow (object sender, View.ViewDetachedFromWindowEventArgs e)
        {
            Unsubscribe (ServiceContainer.Resolve<MessageBus> ());
            base.OnRootDetachedFromWindow (sender, e);
        }

        protected virtual void Subscribe (MessageBus bus)
        {
            if (subscriptionModelChanged == null) {
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (DispatchModelChanged);
            }
        }

        protected virtual void Unsubscribe (MessageBus bus)
        {
            if (subscriptionModelChanged != null) {
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }
        }

        private void DispatchModelChanged (ModelChangedMessage msg)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero)
                return;

            OnModelChanged (msg);
        }

        protected abstract void OnModelChanged (ModelChangedMessage msg);
    }
}

