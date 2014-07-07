using Android.Views;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Joey.UI.Utils
{
    /// <summary>
    /// Model view holder takes care of automatically subscribing and unsubscribing to ModelChangedMessage.
    /// </summary>
    public abstract class ModelViewHolder<T> : BindableViewHolder<T>
    {
        private PropertyChangeTracker tracker = new PropertyChangeTracker ();

        public ModelViewHolder (View root) : base (root)
        {
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (tracker != null) {
                    tracker.Dispose ();
                    tracker = null;
                }
            }

            base.Dispose (disposing);
        }

        protected override void OnRootDetachedFromWindow (object sender, View.ViewDetachedFromWindowEventArgs e)
        {
            tracker.ClearAll ();
            base.OnRootDetachedFromWindow (sender, e);
        }

        protected PropertyChangeTracker Tracker {
            get { return tracker; }
        }

        protected abstract void ResetTrackedObservables ();
    }
}
