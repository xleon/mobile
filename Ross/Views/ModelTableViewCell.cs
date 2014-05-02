using System;
using MonoTouch.UIKit;
using XPlatUtils;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;

namespace Toggl.Ross.Views
{
    public abstract class ModelTableViewCell<T> : BindableTableViewCell<T>
        where T : Model
    {
        private Subscription<ModelChangedMessage> subscriptionModelChanged;

        protected ModelTableViewCell (IntPtr handle) : base (handle)
        {
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (subscriptionModelChanged != null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    bus.Unsubscribe (subscriptionModelChanged);
                    subscriptionModelChanged = null;
                }
            }

            base.Dispose (disposing);
        }

        public override void WillMoveToSuperview (UIView newsuper)
        {
            base.WillMoveToSuperview (newsuper);

            if (newsuper != null) {
                if (subscriptionModelChanged == null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> ((msg) => {
                        if (Handle == IntPtr.Zero)
                            return;

                        OnModelChanged (msg);
                    });
                }
            } else {
                if (subscriptionModelChanged != null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    bus.Unsubscribe (subscriptionModelChanged);
                    subscriptionModelChanged = null;
                }
            }
        }

        protected abstract void OnModelChanged (ModelChangedMessage msg);
    }
}
