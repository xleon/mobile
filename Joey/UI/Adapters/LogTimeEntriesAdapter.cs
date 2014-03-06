using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Adapters
{
    public class LogTimeEntriesAdapter : BaseModelsViewAdapter<TimeEntryModel>
    {
        protected static readonly int ViewTypeDateHeader = ViewTypeContent + 1;
        private readonly List<HeaderPosition> headers = new List<HeaderPosition> ();

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
            return GetHeaderAt (position) == null;
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

            if (GetHeaderAt (position) == null)
                return ViewTypeContent;
            else
                return ViewTypeDateHeader;
        }

        public override int ViewTypeCount {
            get { return base.ViewTypeCount + 1; }
        }

        public override int Count {
            get {
                return base.Count + headers.Count;
            }
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            var model = GetModel (position);

            if (GetHeaderAt (position) != null) {
                TextView headerTextView;
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                    headerTextView = view.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).SetFont (Font.RobotoLight);
                } else {
                    headerTextView = view.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView);
                }
                headerTextView.Text = GetHeaderAt (position).Date.ToShortDateString ();
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

            public Button ContinueButton { get; private set; }

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
                ContinueButton = root.FindViewById<Button> (Resource.Id.ContinueButton);

                ContinueButton.Click += OnContinueButtonClicked;
            }

            void OnContinueButtonClicked (object sender, EventArgs e)
            {
                if (Model == null)
                    return;
                Model.Continue ();
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
                    ProjectTextView.Text = Model.Project.Name;
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
    }
}
