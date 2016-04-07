using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class RecyclerCollectionDataAdapter<T> : RecyclerView.Adapter
    {
        public enum RecyclerLoadState
        {
            Loading,
            Retry,
            Finished
        }

        public const int ViewTypeLoaderPlaceholder = 0;
        public const int ViewTypeContent = 1;
        protected RecyclerView Owner;
        private readonly ObservableCollection<T> collectionData;

        // Protect RecyclerCollectionDataAdapter from
        // requirements when the Collection is null.
        protected ObservableCollection<T> Collection
        {
            get
            {
                if (collectionData == null)
                {
                    return new ObservableCollection<T> ();
                }
                return collectionData;
            }
        }

        protected RecyclerCollectionDataAdapter(IntPtr a, Android.Runtime.JniHandleOwnership b) : base(a, b)
        {
        }

        protected RecyclerCollectionDataAdapter(RecyclerView owner, ObservableCollection<T> collectionData)
        {
            this.collectionData = collectionData;
            Collection.CollectionChanged += OnCollectionChanged;
            Owner = owner;
            HasStableIds = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Collection.CollectionChanged -= OnCollectionChanged;
            }

            base.Dispose(disposing);
        }

        protected void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Handle == IntPtr.Zero)
            {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                NotifyDataSetChanged();
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {

                if (e.NewItems.Count == 1)
                {
                    NotifyItemInserted(e.NewStartingIndex);
                }
                else
                {
                    NotifyItemRangeInserted(e.NewStartingIndex, e.NewItems.Count);
                }

                // Don't scroll when an insert is processed
                // and the scroll position is at top
                var lm = (LinearLayoutManager)Owner.GetLayoutManager();
                if (lm.FindFirstCompletelyVisibleItemPosition() == 0)
                {
                    lm.ScrollToPosition(0);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                NotifyItemChanged(e.NewStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {

                if (e.OldItems.Count == 1)
                {
                    NotifyItemRemoved(e.OldStartingIndex);
                }
                else
                {
                    NotifyItemRangeRemoved(e.OldStartingIndex, e.OldItems.Count);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                NotifyItemMoved(e.OldStartingIndex, e.NewStartingIndex);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            return viewType == ViewTypeLoaderPlaceholder ? GetFooterHolder(parent) : GetViewHolder(parent, viewType);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            BindHolder(holder, position);
        }

        protected T GetItem(int index)
        {
            return index < Collection.Count ? Collection.ElementAt(index) : default (T);
        }

        public override int GetItemViewType(int position)
        {
            return position >= Collection.Count ? ViewTypeLoaderPlaceholder : ViewTypeContent;
        }

        public override int ItemCount
        {
            get
            {
                // Return one element more to return the footer.
                return Collection.Count + 1;
            }
        }

        #region Abstract or Virtual methods
        protected abstract RecyclerView.ViewHolder GetViewHolder(ViewGroup parent, int viewType);

        protected abstract void BindHolder(RecyclerView.ViewHolder holder, int position);

        protected virtual RecyclerView.ViewHolder GetFooterHolder(ViewGroup parent)
        {
            var view = LayoutInflater.FromContext(parent.Context).Inflate(
                           Resource.Layout.TimeEntryListFooter, parent, false);
            return new EmptyFooter(view);
        }
        #endregion

        private class EmptyFooter : RecyclerView.ViewHolder
        {
            public EmptyFooter(View root) : base(root)
            {
                var retryLayout = ItemView.FindViewById<RelativeLayout> (Resource.Id.RetryLayout);
                var progressBar = ItemView.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
                progressBar.Visibility = ViewStates.Invisible;
                retryLayout.Visibility = ViewStates.Invisible;
            }
        }
    }
}
