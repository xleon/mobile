using System;
using System.Collections.Specialized;
using System.Linq;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class RecyclerCollectionDataAdapter<T> : RecyclerView.Adapter
    {
        public enum RecyclerLoadState {
            Loading,
            Retry,
            Finished
        }

        public const int ViewTypeLoaderPlaceholder = 0;
        public const int ViewTypeContent = 1;
        protected ICollectionData<T> CollectionData;
        protected RecyclerView Owner;
        protected RecyclerLoadState currentLoadState = RecyclerLoadState.Loading;

        protected RecyclerCollectionDataAdapter (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        protected RecyclerCollectionDataAdapter (RecyclerView owner, ICollectionData<T> collectionData)
        {
            CollectionData = collectionData;
            CollectionData.CollectionChanged += OnCollectionChanged;
            Owner = owner;
            HasStableIds = false;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                CollectionData.CollectionChanged -= OnCollectionChanged;
            }

            base.Dispose (disposing);
        }

        protected void OnCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Handle == IntPtr.Zero) {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Reset) {
                NotifyDataSetChanged();
            }

            if (e.Action == NotifyCollectionChangedAction.Add) {

                if (e.NewItems.Count == 1) {
                    NotifyItemInserted (e.NewStartingIndex);
                } else {
                    NotifyItemRangeInserted (e.NewStartingIndex, e.NewItems.Count);
                }

                // Don't scroll when an insert is processed
                // and the scroll position is at top
                var lm = (LinearLayoutManager)Owner.GetLayoutManager ();
                if (lm.FindFirstCompletelyVisibleItemPosition () == 0) {
                    lm.ScrollToPosition (0);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                NotifyItemChanged (e.NewStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {

                if (e.OldItems.Count == 1) {
                    NotifyItemRemoved (e.OldStartingIndex);
                } else {
                    NotifyItemRangeRemoved (e.OldStartingIndex, e.OldItems.Count);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                NotifyItemMoved (e.OldStartingIndex, e.NewStartingIndex);
            }
        }

        protected T GetItem (int index)
        {
            return index < CollectionData.Count ? CollectionData.Data.ElementAt (index) : default (T);
        }

        public override int GetItemViewType (int position)
        {
            return position >= CollectionData.Count ? ViewTypeLoaderPlaceholder : ViewTypeContent;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            return viewType == ViewTypeLoaderPlaceholder ? GetFooterHolder (parent) : GetViewHolder (parent, viewType);
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            BindHolder (holder, position);
        }

        public override int ItemCount
        {
            get {
                // Return one element more to return the footer.
                return CollectionData.Count + 1;
            }
        }

        #region Abstract or Virtual methods
        protected abstract RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType);

        protected abstract void BindHolder (RecyclerView.ViewHolder holder, int position);

        protected virtual RecyclerView.ViewHolder GetFooterHolder (ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                           Resource.Layout.TimeEntryListFooter, parent, false);
            return new EmptyFooter (view);
        }
        #endregion

        private class EmptyFooter : RecyclerView.ViewHolder
        {
            public EmptyFooter (View root) : base (root)
            {
                var retryLayout = ItemView.FindViewById<RelativeLayout> (Resource.Id.RetryLayout);
                var progressBar = ItemView.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
                progressBar.Visibility = ViewStates.Invisible;
                retryLayout.Visibility = ViewStates.Invisible;
            }
        }
    }
}
