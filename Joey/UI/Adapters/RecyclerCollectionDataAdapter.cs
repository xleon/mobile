using System;
using System.Collections.Specialized;
using System.Linq;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class RecyclerCollectionDataAdapter<T> : RecyclerView.Adapter
    {
        public const int ViewTypeLoaderPlaceholder = 0;
        public const int ViewTypeContent = 1;
        public const int LoadMoreOffset = 3;

        protected ICollectionData<T> collectionData;
        protected RecyclerView Owner;

        private bool IsInLayout
        {
            get {
                var isInLayout = Owner.GetItemAnimator().IsRunning;
                if (Build.VERSION.SdkInt > BuildVersionCodes.JellyBeanMr1) {
                    isInLayout = Owner.GetItemAnimator().IsRunning || Owner.IsInLayout;
                }
                return isInLayout;
            }
        }

        protected RecyclerCollectionDataAdapter (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        protected RecyclerCollectionDataAdapter (RecyclerView owner, ICollectionData<T> dataView)
        {
            this.collectionData = dataView;
            this.collectionData.CollectionChanged += OnCollectionChanged;
            Owner = owner;

            HasStableIds = false;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (collectionData != null) {
                    collectionData.CollectionChanged -= OnCollectionChanged;
                }
            }
            base.Dispose (disposing);
        }

        private void OnCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Handle == IntPtr.Zero) {
                return;
            }

            CollectionChanged (e);
        }

        public override long GetItemId (int position)
        {
            return -1;
        }

        public virtual T GetEntry (int position)
        {
            return position >= collectionData.Count ? default (T) : collectionData.Data.ElementAt (position);
        }

        public override int GetItemViewType (int position)
        {
            return position >= collectionData.Count ? ViewTypeLoaderPlaceholder : ViewTypeContent;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            return viewType == ViewTypeLoaderPlaceholder ? new SpinnerHolder (GetLoadIndicatorView (parent)) : GetViewHolder (parent, viewType);
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            BindHolder (holder, position);
        }

        protected abstract RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType);

        protected abstract void BindHolder (RecyclerView.ViewHolder holder, int position);

        protected abstract void CollectionChanged (NotifyCollectionChangedEventArgs e);

        public override int ItemCount
        {
            get {
                return collectionData.Count + 1;
            }
        }

        public ICollectionData<T> DataView
        {
            get { return collectionData; }
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
                IsRecyclable = false;
            }

            public virtual void StartAnimation (bool hasMore)
            {
                if (hasMore) {
                    Animation spinningImageAnimation = AnimationUtils.LoadAnimation (ItemView.Context, Resource.Animation.SpinningAnimation);
                    SpinningImage.StartAnimation (spinningImageAnimation);
                } else {
                    SpinningImage.Visibility = ViewStates.Gone;
                }
            }
        }
    }
}
