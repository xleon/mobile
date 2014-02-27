using System;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Graphics;
using System.Collections.Generic;
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
                view = view as LinearLayout;
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                }
                TextView headerTextView = view.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView);
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

        private class TimeEntryListItemHolder : Java.Lang.Object
        {
            private readonly StringBuilder stringBuilder = new StringBuilder ();
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private TimeEntryModel model;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public TextView TaskTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public View TagsView { get; private set; }

            public View BillableView { get; private set; }

            public TextView DurationTextView { get; private set; }

            public Button ContinueButton { get; private set; }

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
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView);
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView);
                TagsView = root.FindViewById<View> (Resource.Id.TagsIcon);
                BillableView = root.FindViewById<View> (Resource.Id.BillableIcon);
                DurationTextView = root.FindViewById<TextView> (Resource.Id.DurationTextView);
                ContinueButton = root.FindViewById<Button> (Resource.Id.ContinueButton);

                ContinueButton.Click += OnContinueButtonClicked;
            }

            void OnContinueButtonClicked (object sender, EventArgs e)
            {
                if (model == null)
                    return;
                model.Continue ();
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                        || msg.PropertyName == TimeEntryModel.PropertyState
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

            private void Rebind ()
            {
                var ctx = ServiceContainer.Resolve<Context> ();

                if (model.Project != null && model.Project.Client != null) {
                    ClientTextView.Text = model.Project.Client.Name;
                } else {
                    ClientTextView.Text = "";
                }

                if (model.Task != null) {
                    //Can't use margin, because with empty task description will still be margined
                    TaskTextView.Text = model.Task.Name + "  ";
                } else {
                    TaskTextView.Text = "";
                }

                var color = Color.Transparent;
                if (model.Project != null) {
                    color = Color.ParseColor (model.Project.GetHexColor ());
                    ProjectTextView.SetTextColor (color);
                    ProjectTextView.Text = model.Project.Name;
                } else {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor (ctx.Resources.GetColor(Resource.Color.dark_gray_text));
                }
                ColorView.SetBackgroundColor (color);

                if (String.IsNullOrWhiteSpace (model.Description)) {
                    DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                } else {
                    DescriptionTextView.Text = model.Description;
                }

                TagsView.Visibility = model.Tags.HasNonDefault ? ViewStates.Visible : ViewStates.Invisible;
                BillableView.Visibility = model.IsBillable ? ViewStates.Visible : ViewStates.Invisible;

                DurationTextView.Text = model.GetDuration ().ToString(@"hh\:mm\:ss"); 
            }
        }
    }
}
