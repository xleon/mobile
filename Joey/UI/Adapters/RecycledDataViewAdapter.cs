using System;
using System.Collections.Specialized;
using System.Linq;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class RecycledDataViewAdapter<T> : RecyclerView.Adapter
    {
        private readonly int LoadMoreOffset = 3;
        protected static readonly int ViewTypeLoaderPlaceholder = 0;
        protected static readonly int ViewTypeContent = 1;
        private CollectionCachingDataView<T> dataView;

        protected RecycledDataViewAdapter (ICollectionDataView<T> dataView)
        {
            this.dataView = new CollectionCachingDataView<T> (dataView);
            this.dataView.Updated += OnDataViewUpdated;
            this.dataView.CollectionChanged += OnCollectionChanged;
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

        private void OnDataViewUpdated (object sender, EventArgs e)
        {
            // Need to access the Handle property, else mono optimises/loses the context and we get a weird
            // low-level exception about "'jobject' must not be IntPtr.Zero".
            if (Handle == IntPtr.Zero) {
                return;
            }
        }

        private void OnCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Handle == IntPtr.Zero) {
                return;
            }
            CollectionChanged (e);
        }

        protected virtual void CollectionChanged (NotifyCollectionChangedEventArgs e)
        {
        }

        public virtual T GetEntry (int position)
        {
            if (dataView.IsLoading && position == dataView.Count) {
                return default (T);
            }
            return dataView.Data.ElementAt (position);
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        public override int GetItemViewType (int position)
        {
            if (position == dataView.Count && dataView.IsLoading) {
                return ViewTypeLoaderPlaceholder;
            }

            return ViewTypeContent;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            return viewType == ViewTypeLoaderPlaceholder ? new SpinnerHolder (GetLoadIndicatorView (parent)) : GetViewHolder (parent, viewType);
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            if (position + LoadMoreOffset > ItemCount && dataView.HasMore && !dataView.IsLoading) {
                //dataView.LoadMore ();
                /*
                Console.WriteLine ( "load More! " + position);
                Console.WriteLine ( "ItemCount! " + ItemCount);
                Console.WriteLine ( "Has More! " + dataView.HasMore);
                Console.WriteLine ( "Loading! " + dataView.IsLoading);
                */
            }

            if (holder is SpinnerHolder) {
                return;
            }

            BindHolder (holder, position);
        }

        protected abstract RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType);

        protected abstract void BindHolder (RecyclerView.ViewHolder holder, int position);

        public override int ItemCount
        {
            get {
                if (dataView.IsLoading) {
                    return (int)dataView.Count + 1;
                }
                return (int)dataView.Count;
            }
        }

        protected CollectionCachingDataView<T> DataView
        {
            get { return dataView; }
        }

        protected virtual View GetLoadIndicatorView (ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                           Resource.Layout.TimeEntryListLoadingItem, parent, false);

            ImageView spinningImage = view.FindViewById<ImageView> (Resource.Id.LoadingImageView);
            Animation spinningImageAnimation = AnimationUtils.LoadAnimation (parent.Context, Resource.Animation.SpinningAnimation);
            spinningImage.StartAnimation (spinningImageAnimation);

            return view;
        }

        protected class SpinnerHolder : RecyclerView.ViewHolder
        {
            public SpinnerHolder (View root) : base (root)
            {
            }
        }
    }
}
