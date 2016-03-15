using Android.Views;

namespace Toggl.Joey.UI.Utils
{
    /// <summary>
    /// Base class for bindable view holders. Useful for list view items.
    /// </summary>
    public abstract class BindableViewHolder<T> : Java.Lang.Object
    {
        private readonly View root;

        public T DataSource { get; private set; }

        // Explanation of native constructor
        // http://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview/10603714#10603714
        protected BindableViewHolder (System.IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        public BindableViewHolder (View root)
        {
            this.root = root;
            root.ViewAttachedToWindow += OnRootAttachedToWindow;
            root.ViewDetachedFromWindow += OnRootDetachedFromWindow;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                root.ViewAttachedToWindow -= OnRootAttachedToWindow;
                root.ViewDetachedFromWindow -= OnRootDetachedFromWindow;

                DataSource = default (T);
            }

            base.Dispose (disposing);
        }

        protected virtual void OnRootAttachedToWindow (object sender, View.ViewAttachedToWindowEventArgs e)
        {
            Rebind ();
        }

        protected virtual void OnRootDetachedFromWindow (object sender, View.ViewDetachedFromWindowEventArgs e)
        {
        }

        public void Bind (T dataSource)
        {
            DataSource = dataSource;
            OnDataSourceChanged ();
        }

        protected virtual void OnDataSourceChanged ()
        {
            Rebind ();
        }

        protected abstract void Rebind ();
    }
}
