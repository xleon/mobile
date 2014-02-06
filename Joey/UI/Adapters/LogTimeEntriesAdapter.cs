using System;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Graphics;
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
    public class LogTimeEntriesAdapter : BaseModelsViewAdapter<TimeEntryModel>
    {
        public LogTimeEntriesAdapter () : base (new AllTimeEntriesView ())
        {
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null) {
                view = LayoutInflater.FromContext (parent.Context).Inflate (
                    Resource.Layout.LogTimeEntryListItem, parent, false);
                view.Tag = new TimeEntryListItemHolder (view);
            }
            var holder = (TimeEntryListItemHolder)view.Tag;
            holder.Bind (GetModel (position));
            return view;
        }

        private class TimeEntryListItemHolder : Java.Lang.Object
        {
            private readonly StringBuilder stringBuilder = new StringBuilder ();
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private TimeEntryModel model;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView DateTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public TextView TagsTextView { get; private set; }

            public TextView BillableTextView { get; private set; }

            public TimeEntryListItemHolder (View root)
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
                DateTextView = root.FindViewById<TextView> (Resource.Id.DateTextView);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView);
                TagsTextView = root.FindViewById<TextView> (Resource.Id.TagsTextView);
                BillableTextView = root.FindViewById<TextView> (Resource.Id.BillableTextView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                        || msg.PropertyName == TimeEntryModel.PropertyIsRunning
                        || msg.PropertyName == TimeEntryModel.PropertyDescription
                        || msg.PropertyName == TimeEntryModel.PropertyProjectId
                        || msg.PropertyName == TimeEntryModel.PropertyTaskId)
                        Rebind ();
                } else if (model.ProjectId.HasValue && model.ProjectId == msg.Model.Id) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor)
                        Rebind ();
                } else if (model.ProjectId.HasValue
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

                ProjectTextView.Text = GetProjectText ();

                var color = Color.Transparent;
                if (model.Project != null) {
                    color = Color.ParseColor (model.Project.GetHexColor());
                }
                ColorView.SetBackgroundColor (color);

                if (String.IsNullOrWhiteSpace (model.Description)) {
                    DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                } else {
                    DescriptionTextView.Text = model.Description;
                }

                // TODO: Use user defined date format
                DateTextView.Text = model.StartTime.ToShortDateString ();

                TagsTextView.Visibility = model.Tags.HasNonDefault ? ViewStates.Visible : ViewStates.Gone;
                BillableTextView.Visibility = model.IsBillable ? ViewStates.Visible : ViewStates.Gone;
            }
        }
    }
}
