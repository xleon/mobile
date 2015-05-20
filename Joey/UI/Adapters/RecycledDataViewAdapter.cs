using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class RecycledDataViewAdapter<T> : RecyclerView.Adapter
    {
        private readonly int ViewTypeLoaderPlaceholder = 0;
        private readonly int ViewTypeContent = 1;
        private readonly int LoadMoreOffset = 3;
        private CollectionCachingDataView<T> dataView;
        private int lastLoadingPosition;
        private UpdateScheduler updateScheduler;
        private int selectedItemIndex = -1;

        protected RecyclerView Owner;

        private bool IsInLayout
        {
            get {
                return Owner.IsInLayout;
            }
        }

        protected RecycledDataViewAdapter (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        protected RecycledDataViewAdapter (RecyclerView owner, ICollectionDataView<T> dataView)
        {
            this.dataView = new CollectionCachingDataView<T> (dataView);
            this.dataView.CollectionChanged += OnCollectionChanged;
            this.dataView.OnIsLoadingChanged += OnLoading;
            this.dataView.OnHasMoreChanged += OnHasMore;
            Owner = owner;

            updateScheduler = new UpdateScheduler (this);
            updateScheduler.UpdateHandler = CollectionChanged;

            HasStableIds = false;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (dataView != null) {
                    var sourceView = dataView.Source as IDisposable;

                    if (sourceView != null) {
                        sourceView.Dispose ();
                    }

                    dataView.Dispose ();
                    dataView = null;
                }
            }
            base.Dispose (disposing);
        }

        private void OnLoading (object sender, EventArgs e)
        {
            // Need to access the Handle property, else mono optimises/loses the context and we get a weird
            // low-level exception about "'jobject' must not be IntPtr.Zero".
            if (Handle == IntPtr.Zero) {
                return;
            }

            // Sometimes a new call to LoadMore is needed.
            if (lastLoadingPosition + LoadMoreOffset > ItemCount && dataView.HasMore && !dataView.IsLoading) {
                dataView.LoadMore();
            }
        }

        private void OnHasMore (object sender, EventArgs e)
        {
            if (Handle == IntPtr.Zero) {
                return;
            }

            if (dataView.HasMore) {
                NotifyItemInserted (dataView.Count);
            } else {
                NotifyItemRemoved (dataView.Count);
            }
        }

        private void OnCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Handle == IntPtr.Zero) {
                return;
            }

            updateScheduler.AddUpdate (e);
        }

        public virtual T GetEntry (int position)
        {
            if (position == dataView.Count) {
                return default (T);
            }
            return dataView.Data.ElementAt (position);
        }

        public override long GetItemId (int position)
        {
            return -1;
        }

        public override int GetItemViewType (int position)
        {
            if (position == dataView.Count) {
                return ViewTypeLoaderPlaceholder;
            }
            return ViewTypeContent;
        }

        public void SetSelectedItem (int itemIndex)
        {
            if (selectedItemIndex != -1) {
                NotifyItemChanged (selectedItemIndex);
            }

            selectedItemIndex = itemIndex;
            NotifyItemChanged (selectedItemIndex);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            return viewType == ViewTypeLoaderPlaceholder ? new SpinnerHolder (GetLoadIndicatorView (parent)) : GetViewHolder (parent, viewType);
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            if (position + LoadMoreOffset > ItemCount && dataView.HasMore && !dataView.IsLoading) {
                lastLoadingPosition = position;
                dataView.LoadMore ();
            }

            if (GetItemViewType (position) == ViewTypeLoaderPlaceholder) {
                var spinnerHolder = (SpinnerHolder)holder;
                spinnerHolder.StartAnimation ();
                return;
            }

            BindHolder (holder, position);
        }

        protected abstract RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType);

        protected abstract void BindHolder (RecyclerView.ViewHolder holder, int position);

        protected abstract void CollectionChanged (NotifyCollectionChangedEventArgs e);

        public override int ItemCount
        {
            get {
                if (dataView.HasMore) {
                    return dataView.Count + 1;
                }
                return dataView.Count;
            }
        }

        public CollectionCachingDataView<T> DataView
        {
            get { return dataView; }
        }

        protected virtual View GetLoadIndicatorView (ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                           Resource.Layout.TimeEntryListLoadingItem, parent, false);
            return view;
        }

        protected class SpinnerHolder : RecyclerView.ViewHolder
        {
            public ImageView SpinningImage { get; set; }

            public SpinnerHolder (View root) : base (root)
            {
                SpinningImage = ItemView.FindViewById<ImageView> (Resource.Id.LoadingImageView);
                IsRecyclable  = false;
            }

            public virtual void StartAnimation()
            {
                Animation spinningImageAnimation = AnimationUtils.LoadAnimation (ItemView.Context, Resource.Animation.SpinningAnimation);
                SpinningImage.StartAnimation (spinningImageAnimation);
            }
        }

        private class UpdateScheduler
        {
            private bool isStarted;
            private RecycledDataViewAdapter<T> owner;
            private readonly Handler handler = new Handler ();
            private readonly List<NotifyCollectionChangedEventArgs> updateQueue = new List<NotifyCollectionChangedEventArgs>();

            public UpdateScheduler (RecycledDataViewAdapter<T> owner)
            {
                this.owner = owner;
            }

            public void AddUpdate (NotifyCollectionChangedEventArgs eventInfo)
            {
                updateQueue.Add (eventInfo);
                if (!isStarted) {
                    isStarted = true;
                    CheckQueue ();
                }
            }

            public Action<NotifyCollectionChangedEventArgs> UpdateHandler;

            private void RunUpdate (NotifyCollectionChangedEventArgs eventInfo)
            {
                UpdateHandler (eventInfo);
            }

            private void CheckQueue()
            {
                if (updateQueue.Count > 0) {
                    if (owner.IsInLayout) {
                        handler.RemoveCallbacks (CheckQueue);
                        handler.PostDelayed (CheckQueue, 10);
                        return;
                    }

                    var evt = updateQueue.First ();
                    RunUpdate (evt);
                    updateQueue.Remove (evt);
                    CheckQueue ();

                } else {
                    isStarted = false;
                }
            }
        }
    }

    public interface IUndoCapabilities
    {
        void RemoveItemWithUndo (int index);

        void RestoreItemFromUndo ();

        void ConfirmItemRemove ();
    }


}
