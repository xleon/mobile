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
            return ExpandedPosition != position && GetEntry (position) is TimeEntryModel;
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
                var model = (TimeEntryModel)entry;
                if (view == null) {
                    view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (
                        Resource.Layout.LogTimeEntryListExpandedItem, parent, false);
                    view.Tag = new ExpandedListItemHolder (this, view);
                }
                var holder = (ExpandedListItemHolder)view.Tag;
                holder.Bind (model);
            } else {
                var model = (TimeEntryModel)entry;
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

                var duration = TimeSpan.FromSeconds (DataSource.Models.Sum (m => m.GetDuration ().TotalSeconds));
                DateGroupDurationTextView.Text = duration.ToString (@"hh\:mm\:ss");

                var runningModel = DataSource.Models.FirstOrDefault (m => m.State == TimeEntryState.Running);
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

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public TextView TaskTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public View TagsView { get; private set; }

            public View BillableView { get; private set; }

            public TextView DurationTextView { get; private set; }

            public ImageButton ContinueImageButton { get; private set; }

            private TimeEntryModel Model {
                get { return DataSource; }
            }

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

            void OnContinueButtonClicked (object sender, EventArgs e)
            {
                if (Model == null)
                    return;
                if (Model.State == TimeEntryState.Running) {
                    adapter.OnStopTimeEntry (Model);
                    return;
                }
                adapter.OnContinueTimeEntry (Model);
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (Model == null)
                    return;

                if (Model == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                        || msg.PropertyName == TimeEntryModel.PropertyState
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
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Visibility = ViewStates.Gone;
                }

                if (Model.Task != null) {
                    TaskTextView.Text = Model.Task.Name;
                    TaskTextView.Visibility = ViewStates.Visible;
                } else {
                    TaskTextView.Visibility = ViewStates.Gone;
                }

                var color = Color.Transparent;
                if (Model.Project != null) {
                    color = Color.ParseColor (Model.Project.GetHexColor ());
                    ProjectTextView.SetTextColor (color);
                    if (String.IsNullOrWhiteSpace (Model.Project.Name)) {
                        ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNamelessProject);
                    } else {
                        ProjectTextView.Text = Model.Project.Name;
                    }
                } else {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor (ctx.Resources.GetColor (Resource.Color.dark_gray_text));
                }

                var shape = ColorView.Background as GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }

                if (String.IsNullOrWhiteSpace (Model.Description)) {
                    if (Model.Task == null) {
                        DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                        DescriptionTextView.Visibility = ViewStates.Visible;
                    } else {
                        DescriptionTextView.Visibility = ViewStates.Gone;
                    }
                } else {
                    DescriptionTextView.Text = Model.Description;
                    DescriptionTextView.Visibility = ViewStates.Visible;
                }

                TagsView.Visibility = Model.Tags.HasNonDefault ? ViewStates.Visible : ViewStates.Gone;
                BillableView.Visibility = Model.IsBillable ? ViewStates.Visible : ViewStates.Gone;

                RebindDuration ();
            }

            private void RebindDuration ()
            {
                if (Model == null || Handle == IntPtr.Zero)
                    return;

                var duration = Model.GetDuration ();
                DurationTextView.Text = duration.ToString (@"hh\:mm\:ss");

                if (Model.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - duration.Milliseconds);
                }
                showStopButton ();
            }

            private void showStopButton()
            {
                if (Model == null || Handle == IntPtr.Zero)
                    return;

                if (Model.State == TimeEntryState.Running) {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcStop);
                } else {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcContinue);
                }
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

            private TimeEntryModel Model {
                get { return DataSource; }
            }

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
                adapter.OnDeleteTimeEntry (Model);
                adapter.ExpandedPosition = null;
            }

            private void OnCloseImageButton (object sender, EventArgs e)
            {
                adapter.ExpandedPosition = null;
            }

            private void OnEditImageButton (object sender, EventArgs e)
            {
                adapter.OnEditTimeEntry (Model);
                adapter.ExpandedPosition = null;
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (Model == null)
                    return;

                if (Model == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                        || msg.PropertyName == TimeEntryModel.PropertyState
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

                RebindProjectTextView (ctx);
                RebindDescriptionTextView (ctx);

                var color = Color.Transparent;
                if (Model.Project != null) {
                    color = Color.ParseColor (Model.Project.GetHexColor ());
                }

                var shape = ColorView.Background as GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }

                if (Model.StopTime.HasValue) {
                    TimeTextView.Text = String.Format ("{0} - {1}",
                        Model.StartTime.ToLocalTime ().ToDeviceTimeString (),
                        Model.StopTime.Value.ToLocalTime ().ToDeviceTimeString ());
                } else {
                    TimeTextView.Text = Model.StartTime.ToLocalTime ().ToDeviceTimeString ();
                }
            }

            private void RebindProjectTextView (Context ctx)
            {
                String text;
                int projectLength = 0;
                int clientLength = 0;
                var mode = SpanTypes.InclusiveExclusive;

                if (Model.Project != null) {
                    var projectName = Model.Project.Name;
                    if (String.IsNullOrWhiteSpace (projectName)) {
                        projectName = ctx.GetString (Resource.String.RecentTimeEntryNamelessProject);
                    }

                    projectLength = projectName.Length;
                    if (Model.Project.Client != null && !String.IsNullOrWhiteSpace (Model.Project.Client.Name)) {
                        clientLength = Model.Project.Client.Name.Length;
                        text = String.Concat (projectName, "   ", Model.Project.Client.Name);
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

                if (String.IsNullOrWhiteSpace (Model.Description)) {
                    text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                    descriptionLength = text.Length;
                } else {
                    text = Model.Description;
                    descriptionLength = Model.Description.Length;
                }

                if (Model.Task != null && !String.IsNullOrEmpty (Model.Task.Name)) {
                    taskLength = Model.Task.Name.Length;
                    text = String.Concat (Model.Task.Name, "  ", text);
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
