using System;
using System.Collections.Specialized;
using System.Linq;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class RecyclerCollectionDataAdapter<T> : RecyclerView.Adapter
    {
        public const int ViewTypeLoaderPlaceholder = 0;
        public const int ViewTypeContent = 1;

        protected ICollectionData<T> CollectionData;
        protected RecyclerView Owner;
        protected bool HasMoreItems { get; set; }

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
                // if position of scroll is at top
                var lm = (LinearLayoutManager)Owner.GetLayoutManager ();
                if (lm.FindFirstCompletelyVisibleItemPosition () == 0) {
                    lm.ScrollToPosition (0);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                NotifyItemChanged (e.NewStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                NotifyItemRemoved (e.OldStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                NotifyItemMoved (e.OldStartingIndex, e.NewStartingIndex);
            }
        }

        protected T GetItem (int index)
        {
            return CollectionData.Data.ElementAt (index);
        }

        public override int GetItemViewType (int position)
        {
            return position >= CollectionData.Count ? ViewTypeLoaderPlaceholder : ViewTypeContent;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            return viewType == ViewTypeLoaderPlaceholder ? new FooterHolder (GetFooterView (parent)) : GetViewHolder (parent, viewType);
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            if (GetItemViewType (position) == ViewTypeLoaderPlaceholder) {
                var footer = (FooterHolder)holder;
                footer.Bind (HasMoreItems);
                return;
            }

            BindHolder (holder, position);
        }

        public override int ItemCount
        {
            get {
                // Return one element more to return the footer.
                return CollectionData.Count + 1;
            }
        }

        #region Abstract methods
        protected abstract RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType);

        protected abstract void BindHolder (RecyclerView.ViewHolder holder, int position);
        #endregion

        protected virtual View GetFooterView (ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                           Resource.Layout.TimeEntryListFooter, parent, false);
            return view;
        }

        protected class FooterHolder : RecyclerView.ViewHolder
        {
            readonly ProgressBar progressBar;

            public FooterHolder (View root) : base (root)
            {
                progressBar = ItemView.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
                IsRecyclable = false;
            }

            public void Bind (bool hasMore)
            {
                progressBar.Visibility = hasMore ? ViewStates.Visible : ViewStates.Invisible;
            }
        }
    }
}
