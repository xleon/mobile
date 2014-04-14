using System;
using System.ComponentModel;
using System.Linq;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Joey.UI.Adapters
{
    public abstract class BaseDataViewAdapter<T> : BaseAdapter
    {
        private static readonly int LoadMoreOffset = 3;
        protected static readonly int ViewTypeLoaderPlaceholder = 0;
        protected static readonly int ViewTypeContent = 1;
        private CachingDataView<T> dataView;

        public BaseDataViewAdapter (IDataView<T> dataView)
        {
            this.dataView = new CachingDataView<T> (dataView);
            this.dataView.Updated += OnDataViewUpdated;
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
            if (Handle == IntPtr.Zero)
                return;
            NotifyDataSetChanged ();
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public virtual T GetEntry (int position)
        {
            if (dataView.IsLoading && position == dataView.Count)
                return default(T);
            return dataView.Data.ElementAt (position);
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        public override bool HasStableIds {
            get { return false; }
        }

        public override int GetItemViewType (int position)
        {
            if (position == dataView.Count && dataView.IsLoading)
                return ViewTypeLoaderPlaceholder;

            return ViewTypeContent;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            if (position + LoadMoreOffset > Count && dataView.HasMore && !dataView.IsLoading) {
                dataView.LoadMore ();
            }

            var viewType = GetItemViewType (position);
            if (viewType == ViewTypeLoaderPlaceholder) {
                return GetLoadIndicatorView (position, convertView, parent);
            } else {
                return GetModelView (position, convertView, parent);
            }
        }

        protected virtual View GetLoadIndicatorView (int position, View convertView, ViewGroup parent)
        {
            if (convertView != null)
                return convertView;

            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                           Resource.Layout.TimeEntryListLoadingItem, parent, false);

            ImageView spinningImage = view.FindViewById<ImageView> (Resource.Id.LoadingImageView);
            Animation spinningImageAnimation = AnimationUtils.LoadAnimation (parent.Context, Resource.Animation.SpinningAnimation);
            spinningImage.StartAnimation (spinningImageAnimation);

            return view;
        }

        protected abstract View GetModelView (int position, View convertView, ViewGroup parent);

        public override int ViewTypeCount {
            get { return 2; }
        }

        public override int Count {
            get {
                if (dataView.IsLoading)
                    return (int)dataView.Count + 1;
                return (int)dataView.Count;
            }
        }

        protected CachingDataView<T> DataView {
            get { return dataView; }
        }
    }
}
