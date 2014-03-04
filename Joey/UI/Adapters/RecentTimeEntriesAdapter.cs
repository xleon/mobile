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
using Toggl.Joey.UI.Utils;

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

        private class RecentTimeEntryListItemHolder : ModelViewHolder<TimeEntryModel>
        {
            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public TextView TaskTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            private TimeEntryModel Model {
                get { return DataSource; }
            }

            public RecentTimeEntryListItemHolder (View root) : base (root)
            {
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView);
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView);
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (Model == null)
                    return;

                if (Model == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyDescription
                        || msg.PropertyName == TimeEntryModel.PropertyProjectId
                        || msg.PropertyName == TimeEntryModel.PropertyTaskId)
                        Rebind ();
                } else if (Model.ProjectId.HasValue && Model.ProjectId == msg.Model.Id) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor)
                        Rebind ();
                } else if (Model.ProjectId.HasValue && Model.Project != null
                           && Model.Project.ClientId.HasValue
                           && Model.Project.ClientId == msg.Model.Id) {
                    if (msg.PropertyName == ClientModel.PropertyName)
                        Rebind ();
                } else if (Model.TaskId.HasValue && Model.TaskId == msg.Model.Id) {
                    if (msg.PropertyName == TaskModel.PropertyName)
                        Rebind ();
                }
            }

            protected override void Rebind ()
            {
                if (Model == null)
                    return;

                var ctx = ServiceContainer.Resolve<Context> ();

                if (Model.Project != null && Model.Project.Client != null) {
                    ClientTextView.Text = Model.Project.Client.Name;
                } else {
                    ClientTextView.Text = "";
                }

                if (Model.Task != null) {
                    //Can't use margin, because with empty task description will still be margined
                    TaskTextView.Text = Model.Task.Name + "  ";
                } else {
                    TaskTextView.Text = "";
                }

                var color = Color.Transparent;
                if (Model.Project != null) {
                    color = Color.ParseColor (Model.Project.GetHexColor ());
                    ProjectTextView.SetTextColor (color);
                    ProjectTextView.Text = Model.Project.Name;
                } else {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor (ctx.Resources.GetColor (Resource.Color.dark_gray_text));
                }

                var shape = ColorView.Background as GradientDrawable;
                shape.SetColor (color);

                if (String.IsNullOrWhiteSpace (Model.Description)) {
                    DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                } else {
                    DescriptionTextView.Text = Model.Description;
                }
            }
        }
    }
}
