using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Android.Util;

namespace Toggl.Joey.UI.Adapters
{
    public class RecentTimeEntriesAdapter : BaseDataViewAdapter<TimeEntryData>
    {
        protected static readonly int ViewTypeFooterView = ViewTypeContent + 1;

        public RecentTimeEntriesAdapter () : base (new RecentTimeEntriesView ())
        {
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            var viewType = GetItemViewType (position);

            if (viewType == ViewTypeContent) {
                if (view == null) {
                    view =  new RecentTimeEntryItem (ServiceContainer.Resolve<Context> (), (IAttributeSet)null );
                    view.Tag = new RecentTimeEntryListItemHolder (view);
                }
                var holder = (RecentTimeEntryListItemHolder)view.Tag;
                holder.Bind ((TimeEntryModel)GetEntry (position));
            } else if (viewType == ViewTypeFooterView) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.RecentTimeEntriesListFooterFragment, parent, false);
                }
            }

            return view;
        }

        public override int Count {
            get {
                if (HasFooterView)
                    return base.Count + 1; // Add virtual footer view
                return base.Count;
            }
        }

        private bool HasFooterView {
            get { return !DataView.IsLoading && !DataView.HasMore && DataView.Count > 0; }
        }

        public override int ViewTypeCount {
            get { return base.ViewTypeCount + 1; }
        }

        public override TimeEntryData GetEntry (int position)
        {
            if (HasFooterView && position == DataView.Count)
                return null;
            return base.GetEntry (position);
        }

        public override bool IsEnabled (int position)
        {
            var type = GetItemViewType (position);
            return type != ViewTypeFooterView && base.IsEnabled (position);
        }

        public override int GetItemViewType (int position)
        {
            if (position == DataView.Count && DataView.IsLoading)
                return ViewTypeLoaderPlaceholder;
            else if (position < 0 || position > DataView.Count)
                throw new ArgumentOutOfRangeException ("position");
            else if (position < DataView.Count)
                return ViewTypeContent;
            else if (HasFooterView && position == DataView.Count)
                return ViewTypeFooterView;

            throw new NotSupportedException ("No view type defined for given object.");
        }

        private class RecentTimeEntryListItemHolder : ModelViewHolder<TimeEntryModel>
        {
            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public TextView TaskTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public RecentTimeEntryListItemHolder (View root) : base (root)
            {
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont (Font.RobotoLight);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont (Font.Roboto);
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView).SetFont (Font.Roboto);
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (DataSource != null) {
                    Tracker.Add (DataSource, HandleTimeEntryPropertyChanged);

                    if (DataSource.Project != null) {
                        Tracker.Add (DataSource.Project, HandleProjectPropertyChanged);

                        if (DataSource.Project.Client != null) {
                            Tracker.Add (DataSource.Project.Client, HandleClientPropertyChanged);
                        }
                    }

                    if (DataSource.Task != null) {
                        Tracker.Add (DataSource.Task, HandleTaskPropertyChanged);
                    }
                }

                Tracker.ClearStale ();
            }

            private void HandleTimeEntryPropertyChanged (string prop)
            {
                if (prop == TimeEntryModel.PropertyProject
                    || prop == TimeEntryModel.PropertyTask
                    || prop == TimeEntryModel.PropertyDescription)
                    Rebind ();
            }

            private void HandleProjectPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyClient
                    || prop == ProjectModel.PropertyColor
                    || prop == ProjectModel.PropertyName)
                    Rebind ();
            }

            private void HandleClientPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyName)
                    Rebind ();
            }

            private void HandleTaskPropertyChanged (string prop)
            {
                if (prop == TaskModel.PropertyName)
                    Rebind ();
            }

            protected override void Rebind ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                ResetTrackedObservables ();

                if (DataSource == null)
                    return;

                var ctx = ServiceContainer.Resolve<Context> ();

                if (DataSource.Project != null && DataSource.Project.Client != null) {
                    ClientTextView.Text = DataSource.Project.Client.Name;
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Visibility = ViewStates.Gone;
                }

                if (DataSource.Task != null) {
                    TaskTextView.Text = DataSource.Task.Name;
                    TaskTextView.Visibility = ViewStates.Visible;
                } else {
                    TaskTextView.Text = String.Empty;
                    TaskTextView.Visibility = ViewStates.Gone;
                }

                var color = Color.Transparent;
                if (DataSource.Project != null) {
                    color = Color.ParseColor (DataSource.Project.GetHexColor ());
                    ProjectTextView.SetTextColor (color);
                    if (String.IsNullOrWhiteSpace (DataSource.Project.Name)) {
                        ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNamelessProject);
                    } else {
                        ProjectTextView.Text = DataSource.Project.Name;
                    }
                } else {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor (ctx.Resources.GetColor (Resource.Color.dark_gray_text));
                }

                var shape = ColorView.Background as GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }

                if (String.IsNullOrWhiteSpace (DataSource.Description)) {
                    if (DataSource.Task == null) {
                        DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                        DescriptionTextView.Visibility = ViewStates.Visible;
                    } else {
                        DescriptionTextView.Visibility = ViewStates.Gone;
                    }
                } else {
                    DescriptionTextView.Text = DataSource.Description;
                    DescriptionTextView.Visibility = ViewStates.Visible;
                }
            }
        }
    }
}
