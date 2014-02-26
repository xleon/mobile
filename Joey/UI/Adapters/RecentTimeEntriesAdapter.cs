using System;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public class RecentTimeEntriesAdapter : BaseModelsViewAdapter<TimeEntryModel>
    {
        public RecentTimeEntriesAdapter () : base (new RecentTimeEntriesView ())
        {
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null) {
                view = LayoutInflater.FromContext (parent.Context).Inflate (
                    Resource.Layout.RecentTimeEntryListItem, parent, false);
                view.Tag = new RecentTimeEntryListItemHolder (view);
            }
            var holder = (RecentTimeEntryListItemHolder)view.Tag;
            holder.Bind (GetModel (position));
            return view;
        }

        private class RecentTimeEntryListItemHolder : Java.Lang.Object
        {
            private readonly StringBuilder stringBuilder = new StringBuilder ();
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private TimeEntryModel model;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public RecentTimeEntryListItemHolder (View root)
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
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyDescription
                        || msg.PropertyName == TimeEntryModel.PropertyProjectId
                        || msg.PropertyName == TimeEntryModel.PropertyTaskId)
                        Rebind ();
                } else if (model.ProjectId.HasValue && model.ProjectId == msg.Model.Id) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor)
                        Rebind ();
                } else if (model.ProjectId.HasValue && model.Project != null
                           && model.Project.ClientId.HasValue
                           && model.Project.ClientId == msg.Model.Id) {
                    if (msg.PropertyName == ClientModel.PropertyName)
                        Rebind ();
                } else if (model.TaskId.HasValue && model.TaskId == msg.Model.Id) {
                    if (msg.PropertyName == TaskModel.PropertyName)
                        Rebind ();
                }
            }

            public void Bind (TimeEntryModel model)
            {
                this.model = model;
                Rebind ();
            }

            private string GetProjectText ()
            {
                var ctx = ServiceContainer.Resolve<Context> ();
                stringBuilder.Clear ();

                if (model.Project == null) {
                    return ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                }

                if (model.Project.Client != null
                    && !String.IsNullOrWhiteSpace (model.Project.Client.Name)) {
                    stringBuilder.Append (model.Project.Client.Name);
                }

                if (!String.IsNullOrWhiteSpace (model.Project.Name)) {
                    if (stringBuilder.Length > 0) {
                        stringBuilder.Append (" - ");
                    }
                    stringBuilder.Append (model.Project.Name);
                }

                if (model.Task != null
                    && !String.IsNullOrWhiteSpace (model.Task.Name)) {
                    if (stringBuilder.Length > 0) {
                        stringBuilder.Append (" | ");
                    }
                    stringBuilder.Append (model.Task.Name);
                }

                return stringBuilder.ToString ();
            }

            private void Rebind ()
            {
                var ctx = ServiceContainer.Resolve<Context> ();

                if (model.Project == null) {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                } else if (model.Task != null) {
                    ProjectTextView.Text = String.Format ("{0} | {1}", model.Project.Name, model.Task.Name);
                } else {
                    ProjectTextView.Text = model.Project.Name;
                }

                var color = Color.Transparent;
                if (model.Project != null) {
                    color = Color.ParseColor (model.Project.GetHexColor ());
                }

                var shape = ColorView.Background as GradientDrawable;
                shape.SetColor (color);

                if (String.IsNullOrWhiteSpace (model.Description)) {
                    DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                } else {
                    DescriptionTextView.Text = model.Description;
                }
            }
        }
    }
}
