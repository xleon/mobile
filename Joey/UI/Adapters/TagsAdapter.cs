using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Adapters
{
    public class TagsAdapter : BaseModelsViewAdapter<TagModel>
    {
        public TagsAdapter (IModelsView<TagModel> view) : base (view)
        {
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null) {
                view = LayoutInflater.FromContext (parent.Context).Inflate (
                    Resource.Layout.TagListItem, parent, false);
                view.Tag = new TagListItemHolder (view);
            }
            var holder = (TagListItemHolder)view.Tag;
            holder.Bind (GetModel (position));
            return view;
        }

        private class TagListItemHolder : ModelViewHolder<TagModel>
        {
            private TagModel Model {
                get { return DataSource; }
            }

            public CheckedTextView NameCheckedTextView { get; private set; }

            public TagListItemHolder (View root) : base (root)
            {
                NameCheckedTextView = root.FindViewById<CheckedTextView> (Resource.Id.NameCheckedTextView).SetFont (Font.Roboto);
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (Model == null)
                    return;

                if (Model == msg.Model) {
                    if (msg.PropertyName == TagModel.PropertyName) {
                        Rebind ();
                    }
                }
            }

            protected override void Rebind ()
            {
                if (Model == null)
                    return;

                NameCheckedTextView.Text = Model.Name;
            }
        }
    }
}
    