using System;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Android.Graphics;

namespace Toggl.Joey.UI.Adapters
{
    public class ProjectsAdapter : BaseModelsViewAdapter<ProjectModel>
    {
        protected static readonly int ViewTypeNoProject = ViewTypeContent + 1;

        public ProjectsAdapter (IModelsView<ProjectModel> view) : base (view)
        {
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            var viewType = GetItemViewType (position);
            if (viewType == ViewTypeNoProject) {
                return GetNoProjectView (position, convertView, parent);
            }

            View view = convertView;
            if (view == null) {
                view = LayoutInflater.FromContext (parent.Context).Inflate (
                    Resource.Layout.ProjectListItem, parent, false);
                view.Tag = new ProjectListItemHolder (view);
            }
            var holder = (ProjectListItemHolder)view.Tag;
            holder.Bind (GetModel (position));
            return view;
        }

        public override int GetItemViewType (int position)
        {
            if (position == 0)
                return ViewTypeNoProject;
            return base.GetItemViewType (position);
        }

        public override ProjectModel GetModel (int position)
        {
            if (position == 0)
                return null;

            return base.GetModel (position - 1);
        }

        protected View GetNoProjectView (int position, View convertView, ViewGroup parent)
        {
            var view = convertView;

            if (view == null) {
                view = LayoutInflater.FromContext (parent.Context).Inflate (
                    Resource.Layout.ProjectListItem, parent, false);

                var colorView = view.FindViewById<View> (Resource.Id.ColorView);
                var projectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);
                var clientTextView = view.FindViewById<TextView> (Resource.Id.ClientTextView);

                colorView.SetBackgroundColor (Color.Gray);
                projectTextView.Text = parent.Context.Resources.GetString (Resource.String.ProjectsNoProject);
                clientTextView.Visibility = ViewStates.Gone;
            }

            return view;
        }

        public override int ViewTypeCount {
            get { return base.ViewTypeCount + 1; }
        }

        public override int Count {
            get { return base.Count + 1; }
        }

        private class ProjectListItemHolder : Java.Lang.Object
        {
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private ProjectModel model;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public ProjectListItemHolder (View root)
            {
                FindViews (root);

                // Cannot use model.OnPropertyChanged callback directly as it would most probably leak memory,
                // thus the global ModelChangedMessage is used instead.
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void FindViews (View root)
            {
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor
                        || msg.PropertyName == ProjectModel.PropertyClientId)
                        Rebind ();
                } else if (model.ClientId.HasValue && model.ClientId == msg.Model.Id) {
                    if (msg.PropertyName == ClientModel.PropertyName)
                        Rebind ();
                }
            }

            public void Bind (ProjectModel model)
            {
                this.model = model;
                Rebind ();
            }

            private void Rebind ()
            {
                var ctx = ProjectTextView.Context;

                var color = Android.Graphics.Color.ParseColor (model.Color.Hex);
                ColorView.SetBackgroundColor (color);

                ProjectTextView.Text = model.Name;

                if (model.Client != null) {
                    ClientTextView.Text = model.Client.Name;
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Visibility = ViewStates.Gone;
                }
            }
        }
    }
}
