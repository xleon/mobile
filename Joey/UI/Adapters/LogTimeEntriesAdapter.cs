using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Android.Provider;
using Android.Text;
using Android.Text.Style;
using Toggl.Joey.UI.Text;

namespace Toggl.Joey.UI.Adapters
{
    public class LogTimeEntriesAdapter : BaseModelsViewAdapter<TimeEntryModel>
    {
        protected static readonly int ViewTypeDateHeader = ViewTypeContent + 1;
        protected static readonly int ViewTypeExpanded = ViewTypeContent + 2;
        private readonly List<HeaderPosition> headers = new List<HeaderPosition> ();
        private int? expandedPos;

        private class HeaderPosition
        {
            public int Position { get; set; }

            public DateTime Date { get; set; }

            public HeaderPosition (int position, DateTime date)
            {
                this.Position = position;
                this.Date = date;
            }
        }

        public LogTimeEntriesAdapter () : base (new AllTimeEntriesView ())
        {
            UpdateHeaders ();
        }

        public override void NotifyDataSetChanged ()
        {
            UpdateHeaders ();
            base.NotifyDataSetChanged ();
        }

        private void UpdateHeaders ()
        {
            headers.Clear ();

            if (ModelsView.Count == 0)
                return;

            TimeEntryModel model = ModelsView.Models.ElementAt (0);

            headers.Add (new HeaderPosition (0, model.StartTime));

            TimeEntryModel prevModel = model;

            for (int i = 1; i < ModelsView.Count; i++) {
                model = ModelsView.Models.ElementAt (i);
                var date = model.StartTime.Date;
                var isNewDay = prevModel.StartTime.Date.CompareTo (date) != 0;
                if (isNewDay) {
                    headers.Add (new HeaderPosition (i + headers.Count, date));
                }
                prevModel = model;
            }
        }

        private HeaderPosition GetHeaderAt (int position)
        {
            return headers.FirstOrDefault ((h) => h.Position == position);
        }

        private int GetIndexForPosition (int position)
        {
            if (GetHeaderAt (position) != null) // This is header position, there is no index for that
                return GetIndexForPosition (position + 1); // Return next position (which is probably an actual item)

            int numberSections = 0;
            foreach (HeaderPosition header in headers) {
                if (position > header.Position) {
                    numberSections++;
                }
            }

            return position - numberSections;
        }

        public override bool IsEnabled (int position)
        {
            return GetHeaderAt (position) == null && ExpandedPosition != position;
        }

        public override TimeEntryModel GetModel (int position)
        {
            var modelIndex = GetIndexForPosition (position);
            if (modelIndex >= ModelsView.Models.Count ())
                return null;
            
            return ModelsView.Models.ElementAt (modelIndex);
        }

        public override int GetItemViewType (int position)
        {
            if (GetIndexForPosition (position) == ModelsView.Count && ModelsView.IsLoading)
                return ViewTypeLoaderPlaceholder;

            if (GetHeaderAt (position) == null) {
                if (expandedPos == position) {
                    return ViewTypeExpanded;
                }
                return ViewTypeContent;
            }
            return ViewTypeDateHeader;
        }

        public override int ViewTypeCount {
            get { return base.ViewTypeCount + 2; }
        }

        public override int Count {
            get {
                return base.Count + headers.Count;
            }
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

        public Action<TimeEntryModel> HandleTimeEntryDeletion { get; set; }

        private static string GetRelativeDateString (DateTime dateTime)
        {
            var ctx = ServiceContainer.Resolve<Context> ();
            var ts = DateTime.Now.Date - dateTime.Date;
            switch (ts.Days) {
            case 0:
                return ctx.Resources.GetString (Resource.String.Today);
            case 1:
                return ctx.Resources.GetString (Resource.String.Yesterday);
            case -1:
                return ctx.Resources.GetString (Resource.String.Tomorrow);
            default:
                return dateTime.ToShortDateString ();
            }
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            var model = GetModel (position);
            var viewType = GetItemViewType (position);

            if (viewType == ViewTypeDateHeader) {
                TextView headerTextView;
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                    headerTextView = view.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).SetFont (Font.RobotoLight);
                } else {
                    headerTextView = view.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView);
                }
                headerTextView.Text = GetRelativeDateString (GetHeaderAt (position).Date);
            } else if (viewType == ViewTypeExpanded) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.LogTimeEntryListExpandedItem, parent, false);
                    view.Tag = new ExpandedListItemHolder (this, view);
                }
                var holder = (ExpandedListItemHolder)view.Tag;
                holder.Bind (model);
            } else {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.LogTimeEntryListItem, parent, false);
                    view.Tag = new TimeEntryListItemHolder (view);
                }
                var holder = (TimeEntryListItemHolder)view.Tag;
                holder.Bind (model);
            }

            return view;
        }

        private class TimeEntryListItemHolder : ModelViewHolder<TimeEntryModel>
        {
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

            public TimeEntryListItemHolder (View root) : base (root)
            {
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
                var entry = Model.Continue ();

                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Send (new UserTimeEntryStateChangeMessage (this, entry));
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

                DurationTextView.Text = Model.GetDuration ().ToString (@"hh\:mm\:ss");
            }
        }

        private class ExpandedListItemHolder : ModelViewHolder<TimeEntryModel>
        {
            private readonly bool timeIs24h;
            private readonly LogTimeEntriesAdapter adapter;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public TextView TimeTextView { get; private set; }

            public ImageButton DeleteImageButton { get; private set; }

            public ImageButton CloseImageButton { get; private set; }

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

                var ctx = ServiceContainer.Resolve<Context> ();
                var clockType = Settings.System.GetString (ctx.ContentResolver, Settings.System.Time1224);
                timeIs24h = !(clockType == null || clockType == "12");

                DeleteImageButton.Click += OnDeleteImageButton;
                CloseImageButton.Click += OnCloseImageButton;
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
                    TimeTextView.Text = String.Format ("{0} - {1}", FormatTime (Model.StartTime), FormatTime (Model.StopTime.Value));
                } else {
                    TimeTextView.Text = FormatTime (Model.StartTime);
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

            private string FormatTime (DateTime time)
            {
                time = time.ToLocalTime ();
                if (timeIs24h) {
                    return time.ToString ("HH:mm:ss");
                }
                return time.ToString ("h:mm:ss tt");
            }
        }
    }
}
