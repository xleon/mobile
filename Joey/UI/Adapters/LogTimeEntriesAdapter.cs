using System;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Joey.UI.Text;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Adapters
{
    public class LogTimeEntriesAdapter : BaseDataViewAdapter<object>
    {
        protected static readonly int ViewTypeDateHeader = ViewTypeContent + 1;
        protected static readonly int ViewTypeExpanded = ViewTypeContent + 2;
        private readonly Handler handler = new Handler ();
        private int? expandedPos;

        public LogTimeEntriesAdapter () : base (new AllTimeEntriesView ())
        {
        }

        public override bool IsEnabled (int position)
        {
            return ExpandedPosition != position && GetEntry (position) is TimeEntryData;
        }

        public override int GetItemViewType (int position)
        {
            if (position == DataView.Count && DataView.IsLoading)
                return ViewTypeLoaderPlaceholder;

            var obj = GetEntry (position);
            if (obj is AllTimeEntriesView.DateGroup) {
                return ViewTypeDateHeader;
            }
            if (position == expandedPos) {
                return ViewTypeExpanded;
            }
            return ViewTypeContent;
        }

        public override int ViewTypeCount {
            get { return base.ViewTypeCount + 2; }
        }

        public int? ExpandedPosition {
            get { return expandedPos; }
            set {
                if (expandedPos == value)
                    return;
                expandedPos = value;
                NotifyDataSetChanged ();
            }
        }

        private void OnDeleteTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryDeletion;
            if (handler != null) {
                handler (model);
            }
        }

        private void OnEditTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryEditing;
            if (handler != null) {
                handler (model);
            }
        }

        private void OnContinueTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryContinue;
            if (handler != null) {
                handler (model);
            }
        }

        private void OnStopTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryStop;
            if (handler != null) {
                handler (model);
            }
        }

        public Action<TimeEntryModel> HandleTimeEntryDeletion { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryEditing { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryContinue { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryStop { get; set; }

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
                    view.Tag = new HeaderListItemHolder (handler, view);
                }
                var holder = (HeaderListItemHolder)view.Tag;
                holder.Bind (dateGroup);
            } else if (viewType == ViewTypeExpanded) {
                var data = (TimeEntryData)entry;
                if (view == null) {
                    view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (
                        Resource.Layout.LogTimeEntryListExpandedItem, parent, false);
                    view.Tag = new ExpandedListItemHolder (this, view);
                }
                var holder = (ExpandedListItemHolder)view.Tag;
                holder.Bind ((TimeEntryModel)data);
            } else {
                var data = (TimeEntryData)entry;
                var model = (TimeEntryModel)data;
                if (view == null) {
                    view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (
                        Resource.Layout.LogTimeEntryListItem, parent, false);
                    view.Tag = new TimeEntryListItemHolder (handler, this, view);
                }
                var holder = (TimeEntryListItemHolder)view.Tag;
                holder.Bind (model);
            }

            return view;
        }

        private class HeaderListItemHolder : BindableViewHolder<AllTimeEntriesView.DateGroup>
        {
            private readonly Handler handler;

            public TextView DateGroupTitleTextView { get; private set; }

            public TextView DateGroupDurationTextView { get; private set; }

            public HeaderListItemHolder (Handler handler, View root) : base (root)
            {
                this.handler = handler;
                DateGroupTitleTextView = root.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).SetFont (Font.RobotoLight);
                DateGroupDurationTextView = root.FindViewById<TextView> (Resource.Id.DateGroupDurationTextView).SetFont (Font.RobotoLight);
            }

            protected override void Rebind ()
            {
                DateGroupTitleTextView.Text = GetRelativeDateString (DataSource.Date);
                RebindDuration ();
            }

            private void RebindDuration ()
            {
                if (DataSource == null || Handle == IntPtr.Zero)
                    return;

                var models = DataSource.DataObjects.Select (data => new TimeEntryModel (data)).ToList ();
                var duration = TimeSpan.FromSeconds (models.Sum (m => m.GetDuration ().TotalSeconds));
                DateGroupDurationTextView.Text = duration.ToString (@"hh\:mm\:ss");

                var runningModel = models.FirstOrDefault (m => m.State == TimeEntryState.Running);
                if (runningModel != null) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - runningModel.GetDuration ().Milliseconds);
                }
            }

            private static string GetRelativeDateString (DateTime dateTime)
            {
                var ctx = ServiceContainer.Resolve<Context> ();
                var ts = Time.Now.Date - dateTime.Date;
                switch (ts.Days) {
                case 0:
                    return ctx.Resources.GetString (Resource.String.Today);
                case 1:
                    return ctx.Resources.GetString (Resource.String.Yesterday);
                case -1:
                    return ctx.Resources.GetString (Resource.String.Tomorrow);
                default:
                    return dateTime.ToDeviceDateString ();
                }
            }
        }

        private class TimeEntryListItemHolder : ModelViewHolder<TimeEntryModel>
        {
            private readonly Handler handler;
            private readonly LogTimeEntriesAdapter adapter;
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

            public TimeEntryListItemHolder (Handler handler, LogTimeEntriesAdapter adapter, View root) : base (root)
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
                if (DataSource == null)
                    return;

                if (DataSource.State == TimeEntryState.Running) {
                    adapter.OnStopTimeEntry (DataSource);
                    return;
                }
                adapter.OnContinueTimeEntry (DataSource);
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
                    || prop == TimeEntryModel.PropertyState
                    || prop == TimeEntryModel.PropertyStartTime
                    || prop == TimeEntryModel.PropertyStopTime
                    || prop == TimeEntryModel.PropertyDescription
                    || prop == TimeEntryModel.PropertyIsBillable)
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

                BillableView.Visibility = DataSource.IsBillable ? ViewStates.Visible : ViewStates.Gone;

                RebindDuration ();
            }

            private void RebindDuration ()
            {
                if (DataSource == null || Handle == IntPtr.Zero)
                    return;

                var duration = DataSource.GetDuration ();
                DurationTextView.Text = duration.ToString (@"hh\:mm\:ss");

                if (DataSource.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - duration.Milliseconds);
                }
                ShowStopButton ();
            }

            private void ShowStopButton ()
            {
                if (DataSource == null || Handle == IntPtr.Zero)
                    return;

                if (DataSource.State == TimeEntryState.Running) {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcStop);
                } else {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcContinue);
                }
            }

            private void RebindTags ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                var showTags = tagsView != null && tagsView.HasNonDefault;
                TagsView.Visibility = showTags ? ViewStates.Visible : ViewStates.Gone;
            }
        }

        private class ExpandedListItemHolder : ModelViewHolder<TimeEntryModel>
        {
            private readonly LogTimeEntriesAdapter adapter;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public TextView TimeTextView { get; private set; }

            public ImageButton DeleteImageButton { get; private set; }

            public ImageButton CloseImageButton { get; private set; }

            public ImageButton EditImageButton { get; private set; }

            public ExpandedListItemHolder (LogTimeEntriesAdapter adapter, View root) : base (root)
            {
                this.adapter = adapter;

                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView);
                TimeTextView = root.FindViewById<TextView> (Resource.Id.TimeTextView).SetFont (Font.RobotoLight);
                DeleteImageButton = root.FindViewById<ImageButton> (Resource.Id.DeleteImageButton);
                CloseImageButton = root.FindViewById<ImageButton> (Resource.Id.CloseImageButton);
                EditImageButton = root.FindViewById<ImageButton> (Resource.Id.EditImageButton);

                DeleteImageButton.Click += OnDeleteImageButton;
                CloseImageButton.Click += OnCloseImageButton;
                EditImageButton.Click += OnEditImageButton;
            }

            private void OnDeleteImageButton (object sender, EventArgs e)
            {
                adapter.OnDeleteTimeEntry (DataSource);
                adapter.ExpandedPosition = null;
            }

            private void OnCloseImageButton (object sender, EventArgs e)
            {
                adapter.ExpandedPosition = null;
            }

            private void OnEditImageButton (object sender, EventArgs e)
            {
                adapter.OnEditTimeEntry (DataSource);
                adapter.ExpandedPosition = null;
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
                    || prop == TimeEntryModel.PropertyStartTime
                    || prop == TimeEntryModel.PropertyStopTime
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
                if (prop == ClientModel.PropertyName)
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

                RebindProjectTextView (ctx);
                RebindDescriptionTextView (ctx);

                var color = Color.Transparent;
                if (DataSource.Project != null) {
                    color = Color.ParseColor (DataSource.Project.GetHexColor ());
                }

                var shape = ColorView.Background as GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }

                if (DataSource.StopTime.HasValue) {
                    TimeTextView.Text = String.Format ("{0} - {1}",
                        DataSource.StartTime.ToLocalTime ().ToDeviceTimeString (),
                        DataSource.StopTime.Value.ToLocalTime ().ToDeviceTimeString ());
                } else {
                    TimeTextView.Text = DataSource.StartTime.ToLocalTime ().ToDeviceTimeString ();
                }
            }

            private void RebindProjectTextView (Context ctx)
            {
                String text;
                int projectLength = 0;
                int clientLength = 0;
                var mode = SpanTypes.InclusiveExclusive;

                if (DataSource.Project != null) {
                    var projectName = DataSource.Project.Name;
                    if (String.IsNullOrWhiteSpace (projectName)) {
                        projectName = ctx.GetString (Resource.String.RecentTimeEntryNamelessProject);
                    }

                    projectLength = projectName.Length;
                    if (DataSource.Project.Client != null && !String.IsNullOrWhiteSpace (DataSource.Project.Client.Name)) {
                        clientLength = DataSource.Project.Client.Name.Length;
                        text = String.Concat (projectName, "   ", DataSource.Project.Client.Name);
                    } else {
                        text = projectName;
                    }
                } else {
                    text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    projectLength = text.Length;
                }

                var start = 0;
                var end = start + projectLength;

                var spannable = new SpannableString (text);
                spannable.SetSpan (new FontSpan (Font.Roboto), start, end, mode);
                spannable.SetSpan (new AbsoluteSizeSpan (18, true), start, end, mode);
                if (clientLength > 0) {
                    start = projectLength + 3;
                    end = start + clientLength;

                    spannable.SetSpan (new FontSpan (Font.RobotoLight), start, end, mode);
                    spannable.SetSpan (new AbsoluteSizeSpan (14, true), start, end, mode);
                }
                ProjectTextView.SetText (spannable, TextView.BufferType.Spannable);
            }

            private void RebindDescriptionTextView (Context ctx)
            {
                String text;
                int taskLength = 0;
                int descriptionLength = 0;
                var mode = SpanTypes.InclusiveExclusive;

                if (String.IsNullOrWhiteSpace (DataSource.Description)) {
                    text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                    descriptionLength = text.Length;
                } else {
                    text = DataSource.Description;
                    descriptionLength = DataSource.Description.Length;
                }

                if (DataSource.Task != null && !String.IsNullOrEmpty (DataSource.Task.Name)) {
                    taskLength = DataSource.Task.Name.Length;
                    text = String.Concat (DataSource.Task.Name, "  ", text);
                }

                var spannable = new SpannableString (text);
                var start = 0;
                var end = taskLength;

                if (taskLength > 0) {
                    spannable.SetSpan (new FontSpan (Font.Roboto), start, end, mode);
                }

                start = taskLength > 0 ? taskLength + 2 : 0;
                end = start + descriptionLength;
                spannable.SetSpan (new FontSpan (Font.RobotoLight), start, end, mode);

                DescriptionTextView.SetText (spannable, TextView.BufferType.Spannable);
            }
        }
    }
}
