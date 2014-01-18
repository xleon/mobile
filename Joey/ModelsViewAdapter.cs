using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Joey
{
    class ModelsViewAdapter<T> : BaseAdapter
        where T : Model
    {
        private static readonly int LoadMoreOffset = 3;
        protected static readonly int ViewTypeLoaderPlaceholder = 1;
        protected static readonly int ViewTypeContent = 2;
        private IModelsView<T> modelsView;

        public ModelsViewAdapter (IModelsView<T> modelsView)
        {
            this.modelsView = modelsView;
            modelsView.PropertyChanged += OnModelsViewPropertyChanged;
        }

        private void OnModelsViewPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ModelsView<T>.PropertyCount
                || e.PropertyName == ModelsView<T>.PropertyHasMore
                || e.PropertyName == ModelsView<T>.PropertyModels) {
                NotifyDataSetChanged ();
            }
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return GetModel (position);
        }

        public T GetModel (int position)
        {
            return modelsView.Models.ElementAt (position);
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
            if (position == modelsView.Count && modelsView.IsLoading)
                return ViewTypeLoaderPlaceholder;

            return ViewTypeLoaderPlaceholder;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            if (position + LoadMoreOffset > modelsView.Count && modelsView.HasMore && !modelsView.IsLoading) {
                modelsView.LoadMore ();
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
            if (convertView)
                return convertView;
            // TODO: Implement default loading indicator view
            return new TextView (parent.Context) {
                Text = "Loading more.."
            };
        }

        protected abstract View GetModelView (int position, View convertView, ViewGroup parent);

        public override int ViewTypeCount {
            get { return 2; }
        }

        public override int Count {
            get {
                if (modelsView.IsLoading)
                    return modelsView.Count + 1;
                return modelsView.Count;
            }
        }
    }
}
