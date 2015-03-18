using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public class GroupedTimeEntriesAdapter : BaseDataViewAdapter<object>
    {
        protected static readonly int ViewTypeDateHeader = ViewTypeContent + 1;
        protected static readonly int ViewTypeExpanded = ViewTypeContent + 2;
        private readonly Handler handler = new Handler ();

        public GroupedTimeEntriesAdapter () : base (new AllTimeEntriesView ())
        {
        }

        public override bool IsEnabled (int position)
        {
            return GetEntry (position) is TimeEntryGroup;
        }

        public override int GetItemViewType (int position)
        {
            if (position == DataView.Count && DataView.IsLoading) {
                return ViewTypeLoaderPlaceholder;
            }

            var obj = GetEntry (position);
            if (obj is AllTimeEntriesView.DateGroup) {
                return ViewTypeDateHeader;
            }
            return ViewTypeContent;
        }

        public override int ViewTypeCount
        {
            get { return base.ViewTypeCount + 2; }
        }

        private void OnDeleteTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            var handler = HandleGroupDeletion;
            if (handler != null) {
                handler (entryGroup);
            }
        }

        private void OnEditTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            var handler = HandleGroupEditing;
            if (handler != null) {
                handler (entryGroup);
            }
        }

        private void OnContinueTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            var handler = HandleGroupContinue;
            if (handler != null) {
                handler (entryGroup);
            }
        }

        private void OnStopTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            var handler = HandleGroupStop;
            if (handler != null) {
                handler (entryGroup);
            }
        }

        public Action<TimeEntryGroup> HandleGroupDeletion { get; set; }

        public Action<TimeEntryGroup> HandleGroupEditing { get; set; }

        public Action<TimeEntryGroup> HandleGroupContinue { get; set; }

        public Action<TimeEntryGroup> HandleGroupStop { get; set; }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            var entry = GetEntry (position);
            var viewType = GetItemViewType (position);

            if (viewType == ViewTypeDateHeader) {
                var dateGroup = (AllTimeEntriesView.DateGroup)entry;
                if (view == null) {
                    view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (
                               Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                    view.Tag = new LogTimeEntriesAdapter.HeaderListItemHolder (handler, view);
                }
                var holder = (LogTimeEntriesAdapter.HeaderListItemHolder)view.Tag;
                holder.Bind (dateGroup);
            } else {
                var data = (TimeEntryGroup)entry;
                data.InitModel();

                if (view == null) {
                    view = new LogTimeEntryItem (ServiceContainer.Resolve<Context> (), (IAttributeSet)null);
                    view.Tag = new GroupedListItemHolder (handler, this, view);
                }
                var holder = (GroupedListItemHolder)view.Tag;
                holder.Bind (data);
            }

            return view;
        }

        private class GroupedListItemHolder : ModelViewHolder<TimeEntryGroup>
        {
            private readonly Handler handler;
            private readonly GroupedTimeEntriesAdapter adapter;
            private TimeEntryTagsView tagsView;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public TextView TaskTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public View TagsView { get; private set; }

            public View BillableView { get; private set; }

            public TextView DurationTextView { get; private set; }

            public ImageButton ContinueImageButton { get; private set; }

            public GroupedListItemHolder (Handler handler, GroupedTimeEntriesAdapter adapter, View root) : base (root)
            {
                this.handler = handler;
                this.adapter = adapter;

                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont (Font.Roboto);
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView).SetFont (Font.Roboto);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont (Font.RobotoLight);
                TagsView = root.FindViewById<View> (Resource.Id.TagsIcon);
                BillableView = root.FindViewById<View> (Resource.Id.BillableIcon);
                DurationTextView = root.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont (Font.RobotoLight);
                ContinueImageButton = root.FindViewById<ImageButton> (Resource.Id.ContinueImageButton);

                ContinueImageButton.Click += OnContinueButtonClicked;
            }

            private void OnContinueButtonClicked (object sender, EventArgs e)
            {
                if (DataSource == null) {
                    return;
                }

                if (DataSource.State == TimeEntryState.Running) {
                    adapter.OnStopTimeEntryGroup (DataSource);
                    return;
                }
                adapter.OnContinueTimeEntryGroup (DataSource);
            }

            protected override void OnDataSourceChanged ()
            {
                // Clear out old
                if (tagsView != null) {
                    tagsView.Updated -= OnTagsUpdated;
                    tagsView = null;
                }

                if (DataSource != null) {
                    tagsView = new TimeEntryTagsView (DataSource.Id);
                    tagsView.Updated += OnTagsUpdated;
                }

                RebindTags ();

                base.OnDataSourceChanged ();
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    if (tagsView != null) {
                        tagsView.Updated -= OnTagsUpdated;
                        tagsView = null;
                    }
                }
                base.Dispose (disposing);
            }

            private void OnTagsUpdated (object sender, EventArgs args)
            {
                RebindTags ();
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (DataSource != null && DataSource.Model != null) {
                    Tracker.Add (DataSource.Model, HandleTimeEntryPropertyChanged);

                    if (DataSource.Model.Project != null) {
                        Tracker.Add (DataSource.Model.Project, HandleProjectPropertyChanged);

                        if (DataSource.Model.Project.Client != null) {
                            Tracker.Add (DataSource.Model.Project.Client, HandleClientPropertyChanged);
                        }
                    }

                    if (DataSource.Model.Task != null) {
                        Tracker.Add (DataSource.Model.Task, HandleTaskPropertyChanged);
                    }
                }

                Tracker.ClearStale ();
            }

            private void HandleTimeEntryPropertyChanged (string prop)
            {
                if (prop == TimeEntryModel.PropertyProject
                        || prop == TimeEntryModel.PropertyTask
                        || prop == TimeEntryModel.PropertyState
                        || prop == TimeEntryModel.PropertyStartTime
                        || prop == TimeEntryModel.PropertyStopTime
                        || prop == TimeEntryModel.PropertyDescription
                        || prop == TimeEntryModel.PropertyIsBillable) {
                    Rebind ();
                }
            }

            private void HandleProjectPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyClient
                        || prop == ProjectModel.PropertyColor
                        || prop == ProjectModel.PropertyName) {
                    Rebind ();
                }
            }

            private void HandleClientPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyName) {
                    Rebind ();
                }
            }

            private void HandleTaskPropertyChanged (string prop)
            {
                if (prop == TaskModel.PropertyName) {
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

                if (DataSource == null || DataSource.Model == null) {
                    return;
                }

                var ctx = ServiceContainer.Resolve<Context> ();

                if (DataSource.Model.Project != null && DataSource.Model.Project.Client != null) {
                    ClientTextView.Text = String.Format ("{0} • ", DataSource.Model.Project.Client.Name);
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Visibility = ViewStates.Gone;
                    ClientTextView.Text = String.Empty;
                }

                if (DataSource.Model.Task != null) {
                    TaskTextView.Text = String.Format ("{0} • ", DataSource.Model.Task.Name);
                    TaskTextView.Visibility = ViewStates.Visible;
                } else {
                    TaskTextView.Text = String.Empty;
                    TaskTextView.Visibility = ViewStates.Gone;
                }

                var color = Color.Transparent;
                if (DataSource.Model.Project != null) {
                    color = Color.ParseColor (DataSource.Model.Project.GetHexColor ());
                    ProjectTextView.SetTextColor (color);
                    if (String.IsNullOrWhiteSpace (DataSource.Model.Project.Name)) {
                        ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNamelessProject);
                    } else {
                        ProjectTextView.Text = DataSource.Model.Project.Name;
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
                    if (DataSource.Model.Task == null) {
                        DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                        DescriptionTextView.Visibility = ViewStates.Visible;
                    } else {
                        DescriptionTextView.Visibility = ViewStates.Gone;
                    }
                } else {
                    DescriptionTextView.Text = DataSource.Description;
                    DescriptionTextView.Visibility = ViewStates.Visible;
                }

                BillableView.Visibility = DataSource.Model.IsBillable ? ViewStates.Visible : ViewStates.Gone;

                RebindDuration ();
            }

            private void RebindDuration ()
            {
                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                var duration = DataSource.Duration;
                DurationTextView.Text = DataSource.GetFormattedDuration ();

                if (DataSource.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - DataSource.Model.GetDuration().Milliseconds);
                }
                ShowStopButton ();
            }

            private void ShowStopButton ()
            {
                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                if (DataSource.State == TimeEntryState.Running) {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcStop);
                } else {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcContinue);
                }
            }

            private void RebindTags ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                var showTags = tagsView != null && tagsView.HasNonDefault;
                TagsView.Visibility = showTags ? ViewStates.Visible : ViewStates.Gone;
            }
        }
    }
}
