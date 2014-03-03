using System;
using Android.Views;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Android.Widget;
using XPlatUtils;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;

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

        private class TagListItemHolder : Java.Lang.Object
        {
            #pragma warning disable 0414
            private readonly Subscription<ModelChangedMessage> subscriptionModelChanged;
            #pragma warning restore 0414
            private TagModel model;

            public CheckedTextView NameCheckedTextView { get; private set; }

            public TagListItemHolder (View root)
            {
                FindViews (root);

                // Cannot use model.OnPropertyChanged callback directly as it would most probably leak memory,
                // thus the global ModelChangedMessage is used instead.
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void FindViews (View root)
            {
                NameCheckedTextView = root.FindViewById<CheckedTextView> (Resource.Id.NameCheckedTextView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == TagModel.PropertyName) {
                        Rebind ();
                    }
                }
            }

            public void Bind (TagModel model)
            {
                this.model = model;
                Rebind ();
            }

            private void Rebind ()
            {
                if (model == null)
                    return;

                NameCheckedTextView.Text = model.Name;
            }
        }
    }
}
    