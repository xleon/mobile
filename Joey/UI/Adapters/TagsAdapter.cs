using System;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Adapters
{
    public class TagsAdapter : BaseDataViewAdapter<TagData>
    {
        public const long CreateTagId = -1;
        protected static readonly int ViewTypeCreateTag = ViewTypeContent + 1;

        public TagsAdapter (IDataView<TagData> view) : base (view)
        {
        }

        public override long GetItemId (int position)
        {
            if (!DataView.IsLoading && position == DataView.Count) {
                return CreateTagId;
            }
            return base.GetItemId (position);
        }

        public override int ViewTypeCount
        {
            get {
                return base.ViewTypeCount + 1;
            }
        }

        public override int GetItemViewType (int position)
        {
            if (GetItemId (position) == CreateTagId) {
                return ViewTypeCreateTag;
            }
            return base.GetItemViewType (position);
        }

        public override int Count
        {
            get {
                var count = base.Count;
                if (!DataView.IsLoading) {
                    count += 1;
                }
                return count;
            }
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            var viewType = GetItemViewType (position);

            if (viewType == ViewTypeCreateTag) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                               Resource.Layout.TagListCreateItem, parent, false);
                    view.FindViewById<TextView> (Resource.Id.CreateLabelTextView).SetFont (Font.Roboto);
                }
            } else {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                               Resource.Layout.TagListItem, parent, false);
                    view.Tag = new TagListItemHolder (view);
                }
                var holder = (TagListItemHolder)view.Tag;
                holder.Bind ((TagModel)GetEntry (position));
            }

            return view;
        }

        private class TagListItemHolder : ModelViewHolder<TagModel>
        {
            public CheckedTextView NameCheckedTextView { get; private set; }

            public TagListItemHolder (View root) : base (root)
            {
                NameCheckedTextView = root.FindViewById<CheckedTextView> (Resource.Id.NameCheckedTextView).SetFont (Font.Roboto);
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (DataSource != null) {
                    Tracker.Add (DataSource, HandleTagPropertyChanged);
                }

                Tracker.ClearStale ();
            }

            private void HandleTagPropertyChanged (string prop)
            {
                if (prop == TagModel.PropertyName) {
                    Rebind ();
                }
            }

            protected override void Rebind ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                ResetTrackedObservables ();

                if (DataSource == null) {
                    return;
                }

                NameCheckedTextView.Text = DataSource.Name;
            }
        }
    }
}
